using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] TMP_InputField ipInputField;
    [SerializeField] TextMeshProUGUI statusText;

    [Header("Settings")]
    [SerializeField] string gameSceneName = "SampleScene";
    [SerializeField] ushort port = 7777;

    void Start()
    {
        if (ipInputField != null) ipInputField.text = "127.0.0.1";
        SetStatus("");

        // Show connection failure if we return to this screen mid-game
        NetworkManager.Singleton.OnClientDisconnectCallback += OnDisconnected;
    }

    void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnDisconnected;
    }

    // Wired to the Host button
    public void OnHostClicked()
    {
        SetStatus("Starting host...");
        SetButtons(false);

        NetworkManager.Singleton.StartHost();
        NetworkManager.Singleton.SceneManager.LoadScene(gameSceneName, LoadSceneMode.Single);
    }

    // Wired to the Connect button
    public void OnConnectClicked()
    {
        string ip = ipInputField != null ? ipInputField.text.Trim() : "127.0.0.1";
        if (string.IsNullOrWhiteSpace(ip)) ip = "127.0.0.1";

        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        if (transport == null)
        {
            SetStatus("Error: UnityTransport not found on NetworkManager.");
            return;
        }

        transport.SetConnectionData(ip, port);
        SetStatus($"Connecting to {ip}:{port}...");
        SetButtons(false);

        NetworkManager.Singleton.StartClient();
    }

    void OnDisconnected(ulong clientId)
    {
        // Only care if we were a client that failed to connect
        if (!NetworkManager.Singleton.IsHost)
        {
            SetStatus("Could not connect. Check the IP and try again.");
            SetButtons(true);
        }
    }

    void SetStatus(string msg)
    {
        if (statusText != null) statusText.text = msg;
    }

    void SetButtons(bool interactable)
    {
        // Disable/re-enable buttons while connecting to prevent double-clicks
        var buttons = GetComponentsInChildren<UnityEngine.UI.Button>();
        foreach (var b in buttons) b.interactable = interactable;
    }
}
