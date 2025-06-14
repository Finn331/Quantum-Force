using cowsins;
using UnityEngine;
using UnityEngine.AI;

public class EnemyMelee : MonoBehaviour
{
    [Header("Waypoint Settings")]
    public Transform[] waypoints;
    private int currentWaypointIndex = 0;

    [Header("Navigation")]
    private NavMeshAgent agent;

    [Header("Player Detection")]
    public Transform player;
    public LayerMask playerLayer;
    public float fieldOfView = 120f;
    public float detectionRange = 15f;
    public float attackRange = 2f;

    [Header("Attack Settings")]
    public float meleeDamage = 20f;
    public float attackCooldown = 2f;
    private float lastAttackTime;

    [Header("State Flags")]
    private bool isAttacking = false;
    private bool playerInSight = false;
    private Vector3 rayDirection;
    private bool isRaycasting;

    private Animator animator;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();

        if (waypoints.Length > 0)
        {
            agent.SetDestination(waypoints[currentWaypointIndex].position);
        }
    }

    void Update()
    {
        isRaycasting = false;

        if (player == null)
        {
            Wander();
            return;
        }

        Vector3 directionToPlayer = (player.position - transform.position).normalized;
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        float angleToPlayer = Vector3.Angle(transform.forward, directionToPlayer);

        // Ray origin & target adaptif (untuk crouch)
        Vector3 rayOrigin = transform.position + Vector3.up * 0.5f;
        Vector3 rayTarget = player.position + Vector3.up * 0.5f;
        rayDirection = (rayTarget - rayOrigin).normalized;
        isRaycasting = true;

        playerInSight = false;
        if (distanceToPlayer <= detectionRange && angleToPlayer <= fieldOfView / 2)
        {
            if (Physics.Raycast(rayOrigin, rayDirection, out RaycastHit hit, detectionRange, playerLayer))
            {
                if (hit.transform.CompareTag("Player"))
                {
                    playerInSight = true;
                }
            }
        }

        if (playerInSight)
        {
            HandleChaseOrAttack(distanceToPlayer);
        }
        else
        {
            Wander();
        }
    }

    void HandleChaseOrAttack(float distance)
    {
        // Smooth look at
        Vector3 lookPos = player.position - transform.position;
        lookPos.y = 0;
        Quaternion rot = Quaternion.LookRotation(lookPos);
        transform.rotation = Quaternion.Slerp(transform.rotation, rot, Time.deltaTime * 5f);

        if (distance > attackRange)
        {
            agent.isStopped = false;
            agent.SetDestination(player.position);
            isAttacking = false;
            animator.SetBool("isWalking", true);
        }
        else
        {
            agent.isStopped = true;
            animator.SetBool("isWalking", false);

            if (Time.time - lastAttackTime >= attackCooldown)
            {
                lastAttackTime = Time.time;
                isAttacking = true;
                animator.SetTrigger("attack");
            }
        }
    }

    void Wander()
    {
        if (waypoints.Length == 0) return;

        if (!agent.pathPending && agent.remainingDistance < 0.5f)
        {
            currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Length;
            agent.SetDestination(waypoints[currentWaypointIndex].position);
        }

        animator?.SetBool("isWalking", true);
    }

    // Animation Event
    public void DealMeleeDamage()
    {
        if (!isAttacking) return;

        Collider[] hits = Physics.OverlapSphere(transform.position, attackRange + 0.5f, playerLayer);
        foreach (var hit in hits)
        {
            if (hit.CompareTag("Player"))
            {
                var stats = hit.GetComponent<PlayerStats>();
                if (stats != null)
                {
                    stats.Damage(meleeDamage, false);
                    Debug.Log("Enemy hit the player!");
                }
            }
        }
    }

    void OnDrawGizmos()
    {
        if (isRaycasting)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawRay(transform.position + Vector3.up * 0.5f, rayDirection * detectionRange);
        }

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }

    public void Die()
    {
        Destroy(gameObject);
    }
}
