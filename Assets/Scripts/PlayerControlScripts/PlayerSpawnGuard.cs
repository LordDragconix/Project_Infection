using System.Collections;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Temporarily freezes the Rigidbody on spawn so the player does not fall
/// through the map while scene colliders are still loading.
/// Add this to both the Human and Creature player prefabs.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class PlayerSpawnGuard : NetworkBehaviour
{
    [Tooltip("Seconds to keep physics frozen after spawning. " +
             "Increase if the map still hasn't loaded in time.")]
    [SerializeField] private float guardDuration = 1.5f;

    private Rigidbody _rb;

    public override void OnNetworkSpawn()
    {
        _rb = GetComponent<Rigidbody>();

        // Freeze physics locally on this machine so gravity cannot pull the
        // player through geometry that hasn't initialised its colliders yet.
        _rb.isKinematic = true;
        StartCoroutine(ReleaseAfterDelay());
    }

    private IEnumerator ReleaseAfterDelay()
    {
        yield return new WaitForSeconds(guardDuration);

        // Only unfreeze if we haven't been despawned in the meantime.
        if (_rb != null)
            _rb.isKinematic = false;
    }
}
