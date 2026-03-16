using System.Threading.Tasks;
using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Authentication;

public class UGSBootstrap : MonoBehaviour
{
    public static bool IsReady { get; private set; }

    async void Awake()
    {
        if (IsReady) return; // only init once

        DontDestroyOnLoad(gameObject);

        await InitServicesAsync();
    }

    async Task InitServicesAsync()
    {
        try
        {
            // Initialise UGS
            await UnityServices.InitializeAsync();

            // Anonymous sign-in is fine for your dissertation game
            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }

            Debug.Log($"Signed in as: {AuthenticationService.Instance.PlayerId}");
            IsReady = true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"UGS init failed: {e}");
        }
    }
}
