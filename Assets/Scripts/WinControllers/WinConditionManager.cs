using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class WinConditionManager : NetworkBehaviour
{
    public enum Winner : int { None = 0, Humans = 1, Monsters = 2 }

    [Header("UI (disabled by default in the scene)")]
    [SerializeField] private GameObject humansWinTMPObject;
    [SerializeField] private GameObject monstersWinTMPObject;

    [Header("Scene")]
    [SerializeField] private string endSceneName = "EndScene";
    [SerializeField] private float endSceneDelay = 8f;

    [Header("Rules")]
    [Tooltip("How often (seconds) the server checks for win conditions.")]
    [SerializeField] private float checkInterval = 0.5f;

    private float _nextCheckTime;

    // Replicated to all clients so everyone shows the same result.
    private readonly NetworkVariable<int> _winner =
        new NetworkVariable<int>((int)Winner.None,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

    public bool GameOver => (Winner)_winner.Value != Winner.None;

    public override void OnNetworkSpawn()
    {
        // Ensure UI starts hidden (you said you will keep them disabled anyway).
        SetWinUI((Winner)_winner.Value);

        _winner.OnValueChanged += (_, newValue) =>
        {
            SetWinUI((Winner)newValue);
        };

        // Optional: if already in a win state when someone joins late, they�ll see it.
        SetWinUI((Winner)_winner.Value);
    }

    private void Update()
    {
        if (!IsServer) return;
        if ((Winner)_winner.Value != Winner.None) return;

        if (Time.time >= _nextCheckTime)
        {
            _nextCheckTime = Time.time + checkInterval;
            EvaluateMonsterWin();
        }
    }

    // Monsters win if there are players connected and all their PlayerObjects are monsters.

    private void EvaluateMonsterWin()
    {
        int connectedPlayers = 0;
        int monsterPlayers = 0;

        foreach (var kvp in NetworkManager.Singleton.ConnectedClients)
        {
            var client = kvp.Value;
            if (client == null) continue;

            var playerObj = client.PlayerObject;
            if (playerObj == null) continue;

            connectedPlayers++;

            // Presence of MonsterMarker means this client is currently a monster.
            if (playerObj.GetComponent<MonsterMarker>() != null)
                monsterPlayers++;
        }

        // If nobody is connected, don�t declare a winner.
        if (connectedPlayers == 0) return;

        // All connected players are monsters => monsters win.
        if (monsterPlayers == connectedPlayers)
        {
            DeclareWin(Winner.Monsters);
        }
    }

    // Call this from a timer/escape trigger to make humans win.
    [ServerRpc(RequireOwnership = false)]
    public void TriggerHumansWinServerRpc()
    {
        if ((Winner)_winner.Value != (int)Winner.None) return;
        DeclareWin(Winner.Humans);
    }

    private void DeclareWin(Winner winner)
    {
        if (!IsServer) return;
        if ((Winner)_winner.Value != Winner.None) return;

        _winner.Value = (int)winner;

        if (!string.IsNullOrWhiteSpace(endSceneName))
            StartCoroutine(LoadEndSceneAfterDelay());
    }

    private IEnumerator LoadEndSceneAfterDelay()
    {
        yield return new WaitForSeconds(endSceneDelay);
        NetworkManager.Singleton.SceneManager.LoadScene(endSceneName, LoadSceneMode.Single);
    }

    private void SetWinUI(Winner winner)
    {
        if (humansWinTMPObject != null)
            humansWinTMPObject.SetActive(winner == Winner.Humans);

        if (monstersWinTMPObject != null)
            monstersWinTMPObject.SetActive(winner == Winner.Monsters);
    }
}
