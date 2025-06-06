using UnityEngine;
using UnityEngine.AI;

public class EnemyRanged : MonoBehaviour
{
    [Header("Waypoint Settings")]
    public Transform[] waypoints;
    private int currentWaypointIndex = 0;

    [Header("Navigation")]
    private NavMeshAgent agent;

    [Header("Player Detection")]
    public Transform player;
    public float detectionRange = 15f;
    public LayerMask playerLayer;
    public float attackRange = 10f;
    public float fieldOfView = 120f; // Sudut pandang AI

    [Header("Attack")]
    public GameObject projectilePrefab;
    public Transform shootPoint;
    public float projectileSpeed = 10f;
    public float projectileDamage = 10f;
    public float attackCooldown = 2f;
    private float lastAttackTime;

    // Debug Info
    private Vector3 rayDirection;
    private bool isRaycasting;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();

        if (waypoints.Length > 0)
        {
            agent.SetDestination(waypoints[currentWaypointIndex].position);
        }
    }

    void Update()
    {
        isRaycasting = false; // Reset debug

        if (player == null)
        {
            Wander();
            return;
        }

        Vector3 directionToPlayer = (player.position - transform.position).normalized;
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        float angleToPlayer = Vector3.Angle(transform.forward, directionToPlayer);

        bool playerInFOV = angleToPlayer <= fieldOfView / 2 && distanceToPlayer <= detectionRange;

        bool canSeePlayer = false;
        if (playerInFOV)
        {
            rayDirection = directionToPlayer;
            isRaycasting = true;

            if (Physics.Raycast(transform.position + Vector3.up, directionToPlayer, out RaycastHit hit, detectionRange, playerLayer))
            {
                if (hit.transform.CompareTag("Player"))
                {
                    canSeePlayer = true;
                }
            }
        }

        if (canSeePlayer)
        {
            // Kejar player
            agent.SetDestination(player.position);

            // Jika sudah dalam jarak attackRange, tembak sambil berjalan
            if (distanceToPlayer <= attackRange)
            {
                AttackPlayer(directionToPlayer);
            }
        }
        else
        {
            // Player hilang -> kembali ke wandering
            Wander();
        }
    }

    void Wander()
    {
        if (!agent.pathPending && agent.remainingDistance < 0.5f)
        {
            currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Length;
            agent.SetDestination(waypoints[currentWaypointIndex].position);
        }
    }

    void AttackPlayer(Vector3 direction)
    {
        // Tetap bergerak sambil menembak (tidak reset path!)
        transform.LookAt(new Vector3(player.position.x, transform.position.y, player.position.z));

        if (Time.time - lastAttackTime > attackCooldown)
        {
            lastAttackTime = Time.time;
            ShootProjectile(direction);
        }
    }

    void ShootProjectile(Vector3 direction)
    {
        GameObject projectile = Instantiate(projectilePrefab, shootPoint.position, Quaternion.identity);
        var projectileScript = projectile.GetComponent<cowsins.TurretProjectile>();

        projectileScript.dir = direction;
        projectileScript.damage = projectileDamage;
        projectileScript.speed = projectileSpeed;
    }

    void OnDrawGizmos()
    {
        // Gambar raycast hijau
        if (isRaycasting)
        {
            Gizmos.color = Color.green;
            Vector3 start = transform.position + Vector3.up;
            Gizmos.DrawRay(start, rayDirection * detectionRange);
        }

        // Gambar pandangan AI (biru)
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(transform.position + Vector3.up, transform.forward * detectionRange);
    }

    public void Die()
    {
        Destroy(gameObject);
    }
}
