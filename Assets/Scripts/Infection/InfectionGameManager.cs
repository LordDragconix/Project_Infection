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
    [SerializeField] float initialCreaturePercentage = 0.2f;   // 20% of players
    [SerializeField] int minInitialCreatures = 1;              // at least this many
    [SerializeField] int minPlayersToStart = 2;                // wait until this many players are in

    bool roundStarted = false;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void OnEnable()
    {
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
    }

    void OnDisable()
    {
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
    }

    void OnClientConnected(ulong clientId)
    {
        if (!IsServer || roundStarted)
            return;

        int playerCount = NetworkManager.Singleton.ConnectedClientsList.Count;

        // Auto-start the round when enough players have joined
        if (playerCount >= minPlayersToStart)
        {
            StartInitialInfection();
        }
    }

    /// <summary>
    /// Server-only: randomly turn a percentage of players into creatures.
    /// </summary>
    void StartInitialInfection()
    {
        if (!IsServer || roundStarted)
            return;

        roundStarted = true;

        var clients = NetworkManager.Singleton.ConnectedClientsList;
        int total = clients.Count;
        if (total == 0)
            return;

        int targetCreatures = Mathf.RoundToInt(total * initialCreaturePercentage);
        targetCreatures = Mathf.Max(minInitialCreatures, targetCreatures);
        targetCreatures = Mathf.Min(targetCreatures, total);

        // Build a list of client IDs we can choose from
        List<ulong> candidates = new List<ulong>(total);
        foreach (var c in clients)
        {
            candidates.Add(c.ClientId);
        }

        // Shuffle candidates (Fisher–Yates)
        for (int i = candidates.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (candidates[i], candidates[j]) = (candidates[j], candidates[i]);
        }

        // Pick first N as creatures
        for (int i = 0; i < targetCreatures; i++)
        {
            ulong clientId = candidates[i];

            if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client))
                continue;

            var playerObj = client.PlayerObject;
            if (playerObj == null)
                continue;

            var t = playerObj.transform;
            TurnHumanIntoCreature(clientId, t.position, t.rotation);
        }

        Debug.Log($"[InfectionGameManager] Round started with {targetCreatures}/{total} creatures.");
    }

    /// <summary>
    /// Swaps a client's player object from Human to Creature.
    /// Can be used both for initial infection and later infection.
    /// </summary>
    public void TurnHumanIntoCreature(ulong clientId, Vector3 pos, Quaternion rot)
    {
        if (!IsServer)
            return;

        if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client))
            return;

        NetworkObject oldPlayer = client.PlayerObject;
        if (oldPlayer != null)
        {
            oldPlayer.Despawn(true);
        }

        NetworkObject newPlayer = Instantiate(creaturePrefab, pos, rot);
        newPlayer.SpawnAsPlayerObject(clientId);
    }
}
