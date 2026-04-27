using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class InfectionGameManager : NetworkBehaviour
{
    public static InfectionGameManager Instance { get; private set; }

    [Header("Prefabs")]
    [SerializeField] NetworkObject humanPrefab;
    [SerializeField] NetworkObject creaturePrefab;

    [Header("Round Setup")]
    [Range(0f, 1f)]
    [SerializeField] float initialCreaturePercentage = 0.2f;
    [SerializeField] int minInitialCreatures = 1;
    [SerializeField] int minPlayersToStart = 2;

    [Header("Spawn Points")]
    [SerializeField] Transform[] spawnPoints;

    [Header("Pre-Game")]
    [SerializeField] float preGameDuration = 60f;
    [SerializeField] MatchCountdownTimer matchTimer;

    [Header("UI")]
    [SerializeField] GameObject survivorRevealObject;
    [SerializeField] GameObject infectedRevealObject;
    [SerializeField] float roleRevealDuration = 4f;

    // > 0 once countdown is running; all clients read it to drive UI
    private readonly NetworkVariable<float> _preGameEndTime = new NetworkVariable<float>(0f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    bool roundStarted = false;
    bool countdownStarted = false;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            NetworkManager.OnClientConnectedCallback += OnClientConnected;
            // Reposition all players once the game scene has fully loaded on every client.
            NetworkManager.SceneManager.OnLoadEventCompleted += OnSceneLoadCompleted;
        }

        if (survivorRevealObject != null) survivorRevealObject.SetActive(false);
        if (infectedRevealObject != null) infectedRevealObject.SetActive(false);

        // Use the shared timer TMP for the waiting message on every client.
        if (matchTimer != null && matchTimer.CountdownDisplay != null)
            matchTimer.CountdownDisplay.text = "Waiting for players...";
    }

    public override void OnNetworkDespawn()
    {
        NetworkManager.OnClientConnectedCallback -= OnClientConnected;
        NetworkManager.SceneManager.OnLoadEventCompleted -= OnSceneLoadCompleted;
    }

    void OnSceneLoadCompleted(string sceneName, UnityEngine.SceneManagement.LoadSceneMode mode,
        System.Collections.Generic.List<ulong> completed, System.Collections.Generic.List<ulong> timedOut)
    {
        if (spawnPoints == null || spawnPoints.Length == 0) return;

        // Only the host needs repositioning — clients spawn into the already-loaded scene correctly.
        Vector3 pos = spawnPoints[Random.Range(0, spawnPoints.Length)].position;
        TeleportClientRpc(pos, new ClientRpcParams
        {
            Send = new ClientRpcSendParams { TargetClientIds = new[] { NetworkManager.ServerClientId } }
        });
    }

    [ClientRpc]
    void TeleportClientRpc(Vector3 position, ClientRpcParams _ = default)
    {
        var playerObj = NetworkManager.LocalClient?.PlayerObject;
        if (playerObj != null)
            playerObj.transform.position = position;
    }

    void OnClientConnected(ulong clientId)
    {
        if (!IsServer || countdownStarted) return;

        int playerCount = NetworkManager.Singleton.ConnectedClientsList.Count;
        if (playerCount >= minPlayersToStart)
        {
            countdownStarted = true;
            _preGameEndTime.Value = (float)NetworkManager.Singleton.ServerTime.Time + preGameDuration;
            Debug.Log($"[InfectionGameManager] Pre-game countdown started ({preGameDuration}s).");
        }
    }

    void Update()
    {
        // Nothing to do until the countdown is set.
        if (_preGameEndTime.Value <= 0f) return;

        float now = (float)NetworkManager.Singleton.ServerTime.Time;
        float remaining = _preGameEndTime.Value - now;

        if (remaining > 0f)
        {
            // Still counting down — all clients update the shared TMP.
            if (matchTimer != null && matchTimer.CountdownDisplay != null)
                matchTimer.CountdownDisplay.text = $"Game starts in: {Mathf.CeilToInt(remaining)}s";
        }
        // Once remaining hits 0, this block stops touching the TMP and
        // MatchCountdownTimer.Update() naturally takes over the same object.

        // Server triggers infection as soon as the countdown expires.
        if (IsServer && !roundStarted && remaining <= 0f)
            StartInitialInfection();
    }

    void StartInitialInfection()
    {
        if (!IsServer || roundStarted) return;
        roundStarted = true;

        var clients = NetworkManager.Singleton.ConnectedClientsList;
        int total = clients.Count;
        if (total == 0) return;

        int targetCreatures = Mathf.RoundToInt(total * initialCreaturePercentage);
        targetCreatures = Mathf.Max(minInitialCreatures, targetCreatures);
        targetCreatures = Mathf.Min(targetCreatures, total);

        List<ulong> candidates = new List<ulong>(total);
        foreach (var c in clients) candidates.Add(c.ClientId);

        // Fisher-Yates shuffle
        for (int i = candidates.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (candidates[i], candidates[j]) = (candidates[j], candidates[i]);
        }

        var infectedSet = new HashSet<ulong>();
        for (int i = 0; i < targetCreatures; i++)
        {
            ulong id = candidates[i];
            infectedSet.Add(id);

            if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(id, out var client)) continue;
            var playerObj = client.PlayerObject;
            if (playerObj == null) continue;

            TurnHumanIntoCreature(id, playerObj.transform.position, playerObj.transform.rotation);
        }

        // Tell each client their role.
        foreach (var c in clients)
        {
            bool infected = infectedSet.Contains(c.ClientId);
            ShowRoleClientRpc(infected, new ClientRpcParams
            {
                Send = new ClientRpcSendParams { TargetClientIds = new[] { c.ClientId } }
            });
        }

        // Kick off the main match timer — MatchCountdownTimer now owns the TMP.
        if (matchTimer != null) matchTimer.StartMatchTimer();

        Debug.Log($"[InfectionGameManager] Round started: {targetCreatures}/{total} infected.");
    }

    public void TurnHumanIntoCreature(ulong clientId, Vector3 pos, Quaternion rot)
    {
        if (!IsServer) return;
        if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client)) return;

        NetworkObject oldPlayer = client.PlayerObject;
        if (oldPlayer != null) oldPlayer.Despawn(true);

        NetworkObject newPlayer = Instantiate(creaturePrefab, pos, rot);
        newPlayer.SpawnAsPlayerObject(clientId);
    }

    [ClientRpc]
    void ShowRoleClientRpc(bool isInfected, ClientRpcParams _ = default)
    {
        GameObject toShow = isInfected ? infectedRevealObject : survivorRevealObject;
        if (toShow != null) toShow.SetActive(true);
        StartCoroutine(HideRoleAfterDelay(toShow));
    }

    IEnumerator HideRoleAfterDelay(GameObject obj)
    {
        yield return new WaitForSeconds(roleRevealDuration);
        if (obj != null) obj.SetActive(false);
    }
}
