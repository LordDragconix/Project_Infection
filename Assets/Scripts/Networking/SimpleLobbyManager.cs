using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;

public class SimpleLobbyManager : MonoBehaviour
{
    Lobby currentLobby;

    // Called by your "Host Game" button
    public async void OnHostClicked()
    {
        if (!UGSBootstrap.IsReady) return;

        await CreateLobbyAsync("Infection Lobby", 8);
    }

    // Called by "Refresh" in your Find Games menu
    public async void OnRefreshLobbiesClicked()
    {
        if (!UGSBootstrap.IsReady) return;

        var lobbies = await QueryLobbiesAsync();
        // TODO: populate your UI list from lobbies
        foreach (var lobby in lobbies)
        {
            Debug.Log($"Lobby: {lobby.Id} | {lobby.Name} | {lobby.Players.Count}/{lobby.MaxPlayers}");
        }
    }

    // Called when player clicks a lobby in your UI
    public async void JoinLobbyById(string lobbyId)
    {
        if (!UGSBootstrap.IsReady) return;

        try
        {
            currentLobby = await LobbyService.Instance.JoinLobbyByIdAsync(lobbyId);
            Debug.Log($"Joined lobby {currentLobby.Name}");

            // TODO: after this, connect Netcode (Relay / IP) then load game scene.
        }
        catch (System.Exception e)
        {
            Debug.LogError($"JoinLobby failed: {e}");
        }
    }

    async Task CreateLobbyAsync(string lobbyName, int maxPlayers)
    {
        try
        {
            var options = new CreateLobbyOptions
            {
                // You can store metadata here (map, mode, joinCode, etc.)
                Data = new Dictionary<string, DataObject>()
                {
                    { "mode", new DataObject(DataObject.VisibilityOptions.Public, "Infection") }
                }
            };

            currentLobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayers, options);
            Debug.Log($"Created lobby {currentLobby.Id} | {currentLobby.Name}");

            // TODO: here is where you will also create a Relay allocation
            // and store the join code in lobby.Data (e.g. under "relayJoinCode").
        }
        catch (System.Exception e)
        {
            Debug.LogError($"CreateLobby failed: {e}");
        }
    }

    async Task<List<Lobby>> QueryLobbiesAsync()
    {
        try
        {
            var options = new QueryLobbiesOptions()
            {
                Count = 20,
                Filters = new List<QueryFilter>
                {
                    // Example: only Infection mode
                    new QueryFilter(
                        field: QueryFilter.FieldOptions.S1,
                        op: QueryFilter.OpOptions.EQ,
                        value: "Infection")
                }
            };

            var result = await LobbyService.Instance.QueryLobbiesAsync(options);
            return result.Results;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"QueryLobbies failed: {e}");
            return new List<Lobby>();
        }
    }
}
