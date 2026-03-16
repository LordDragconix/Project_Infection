using Unity.Netcode;
using UnityEngine;

public class CreatureMeleeAttack : NetworkBehaviour
{
    [Header("Attack")]
    [SerializeField] float attackRange = 2f;
    [SerializeField] float attackRadius = 0.6f;
    [SerializeField] int damage = 20;
    [SerializeField] float attackCooldown = 0.6f;
    [SerializeField] LayerMask hitMask; // set to Player layer

    float nextAttackTime;

    PlayerHealth myHealth;

    void Awake()
    {
        myHealth = GetComponent<PlayerHealth>();
    }

    void Update()
    {
        if (!IsOwner)
            return;

        if (Time.time < nextAttackTime)
            return;

        if (Input.GetButtonDown("Fire1"))
        {
            nextAttackTime = Time.time + attackCooldown;

            // send a request to the server with our current origin/forward
            RequestMeleeAttackServerRpc(transform.position, transform.forward);
        }
    }

    [ServerRpc]
    void RequestMeleeAttackServerRpc(Vector3 origin, Vector3 forward, ServerRpcParams rpcParams = default)
    {
        if (!IsServer)
            return;

        // extra safety: only allow creatures to do this
        if (myHealth == null || myHealth.Type != PlayerType.Creature)
            return;

        // server-side hit detection
        Vector3 centre = origin + forward.normalized * attackRange * 0.5f;

        Collider[] hits = Physics.OverlapSphere(centre, attackRadius, hitMask, QueryTriggerInteraction.Ignore);

        foreach (var col in hits)
        {
            var targetHealth = col.GetComponentInParent<PlayerHealth>();
            if (targetHealth == null)
                continue;

            // don't hit yourself
            if (targetHealth.OwnerClientId == OwnerClientId)
                continue;

            // apply damage: mark that attacker is a creature
            targetHealth.TakeDamageServerRpc(damage, OwnerClientId, attackerIsCreature: true);

            // single-target melee; break after first
            break;
        }
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        // For debugging the melee arc in Scene view
        Gizmos.DrawWireSphere(transform.position + transform.forward * attackRange * 0.5f, attackRadius);
    }
#endif
}
