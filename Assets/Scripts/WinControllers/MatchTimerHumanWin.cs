using TMPro;
using Unity.Netcode;
using UnityEngine;

public sealed class MatchCountdownTimer : NetworkBehaviour
{
    [Header("Setup")]
    [SerializeField] private WinConditionManager winManager;
    [SerializeField] private TextMeshProUGUI countdownTMP;

    [Header("Timer")]
    [Tooltip("Match length in seconds.")]
    [SerializeField] private int matchDurationSeconds = 180;

    // Server writes, everyone reads
    private readonly NetworkVariable<float> endTimeServer =
        new NetworkVariable<float>(0f,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

    private bool _ended;

    public override void OnNetworkSpawn() { }

    // Called by InfectionGameManager after initial infection completes.
    public void StartMatchTimer()
    {
        if (!IsServer) return;
        endTimeServer.Value = (float)NetworkManager.Singleton.ServerTime.Time + matchDurationSeconds;
    }

    private void Update()
    {
        if (countdownTMP == null) return;
        if (endTimeServer.Value <= 0f) return;

        // Remaining time based on server clock (clients can compute locally)
        float now = (float)NetworkManager.Singleton.ServerTime.Time;
        float remaining = Mathf.Max(0f, endTimeServer.Value - now);

        countdownTMP.text = FormatMMSS(remaining);

        // Only server triggers win
        if (IsServer && !_ended && remaining <= 0.01f)
        {
            _ended = true;
            winManager.TriggerHumansWinServerRpc();
        }
    }

    private static string FormatMMSS(float seconds)
    {
        int total = Mathf.CeilToInt(seconds);
        int mins = total / 60;
        int secs = total % 60;
        return $"{mins:00}:{secs:00}";
    }
}
