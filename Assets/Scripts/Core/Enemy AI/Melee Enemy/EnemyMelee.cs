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
    public float walkSpeed = 2f;
    public float chaseSpeed = 4f;

    [Header("Player Detection")]
    public Transform player;
    public LayerMask playerLayer;
    public LayerMask obstacleLayerMask;
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

    [Header("Combat Awareness")]
    public bool wasProvoked = false;

    [Header("Footstep Sounds")]
    public AudioClip[] walkFootsteps;
    public AudioClip[] runFootsteps;
    public float footstepVolume = 1f;

    [Header("SFX Settings")]
    public AudioClip hurtSFX;
    [SerializeField] AudioClip deathSFX;

    private Animator animator;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        agent.speed = walkSpeed;

        if (waypoints.Length > 0)
            agent.SetDestination(waypoints[currentWaypointIndex].position);
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

        Vector3 rayOrigin = transform.position + Vector3.up * 0.5f;
        Vector3 rayTarget = player.position + Vector3.up * 0.5f;
        rayDirection = (rayTarget - rayOrigin).normalized;
        isRaycasting = true;

        playerInSight = false;

        if (distanceToPlayer <= detectionRange && angleToPlayer <= fieldOfView / 2)
        {
            RaycastHit[] hits = Physics.RaycastAll(rayOrigin, rayDirection, detectionRange);
            float closestDist = Mathf.Infinity;
            Transform closest = null;

            foreach (RaycastHit hit in hits)
            {
                if (((1 << hit.collider.gameObject.layer) & obstacleLayerMask) != 0)
                    break; // Terhalang oleh dinding atau obstacle

                float d = hit.distance;
                if (d < closestDist)
                {
                    closestDist = d;
                    closest = hit.transform;
                }
            }

            if (closest != null && closest.CompareTag("Player"))
            {
                playerInSight = true;
            }
        }

        if (playerInSight || wasProvoked)
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
        Vector3 lookPos = player.position - transform.position;
        lookPos.y = 0;
        Quaternion rot = Quaternion.LookRotation(lookPos);
        transform.rotation = Quaternion.Slerp(transform.rotation, rot, Time.deltaTime * 5f);

        if (distance > attackRange)
        {
            agent.isStopped = false;
            agent.speed = chaseSpeed;
            agent.SetDestination(player.position);

            isAttacking = false;
            animator.SetBool("isRunning", true);
            animator.SetBool("isWalking", false);
        }
        else
        {
            agent.isStopped = true;
            agent.speed = 0;

            animator.SetBool("isRunning", false);
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

        agent.speed = walkSpeed;

        if (!agent.pathPending && agent.remainingDistance < 0.5f)
        {
            currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Length;
            agent.SetDestination(waypoints[currentWaypointIndex].position);
        }

        animator.SetBool("isWalking", true);
        animator.SetBool("isRunning", false);
    }

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

    public void OnTakeDamage()
    {
        wasProvoked = true;
        SoundManager.Instance.PlaySound(hurtSFX, 0f, 0f, true, 1f);
    }

    public void PlayFootstepSound()
    {
        AudioClip[] selectedClips = animator.GetBool("isRunning") ? runFootsteps : walkFootsteps;
        if (selectedClips.Length == 0) return;

        int index = Random.Range(0, selectedClips.Length);
        AudioClip clip = selectedClips[index];

        SoundManager.Instance.PlaySound(clip, 0f, 0f, true, 0f, footstepVolume);
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
