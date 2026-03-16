using Unity.Netcode;
using UnityEngine;

public enum PlayerType
{
    Human,
    Creature
}

public class PlayerHealth : NetworkBehaviour
{
    [Header("Setup")]
    [SerializeField] PlayerType playerType = PlayerType.Human;
    [SerializeField] int maxHealth = 100;

    [Header("Infection")]
    [Range(0f, 1f)]
    [SerializeField] float infectionThreshold = 0.2f;   // 20%

    public NetworkVariable<int> CurrentHealth = new NetworkVariable<int>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public PlayerType Type => playerType;
    public int MaxHealth => maxHealth;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
            CurrentHealth.Value = maxHealth;
    }

    /// <summary>
    /// Called by server when this player is hit by something.
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void TakeDamageServerRpc(int damage, ulong attackerClientId, bool attackerIsCreature)
    {
        if (!IsServer || damage <= 0)
            return;

        if (CurrentHealth.Value <= 0)
            return; // already dead

        // Apply damage
        int oldHealth = CurrentHealth.Value;
        int newHealth = Mathf.Max(oldHealth - damage, 0);
        CurrentHealth.Value = newHealth;

        // Infection check: only humans can be infected, and only by a creature
        if (playerType == PlayerType.Human && attackerIsCreature)
        {
            float ratio = (float)newHealth / maxHealth;

            // Requirement: HP below 20% *and* attacked by creature
            if (ratio <= infectionThreshold && InfectionGameManager.Instance != null)
            {
                // turn this human into a creature
                InfectionGameManager.Instance.TurnHumanIntoCreature(
                    OwnerClientId,
                    transform.position,
                    transform.rotation
                );
                return;
            }
        }

        // If you want a "normal death" case (non-infection), handle here
        if (CurrentHealth.Value <= 0)
        {
            // TODO: respawn / ragdoll / etc.
        }
    }
}
