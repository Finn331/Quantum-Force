using UnityEngine;
using UnityEngine.AI;
using cowsins;

public class Pulzar : MonoBehaviour
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

    [Header("Combat Awareness")]
    private bool isAttacking = false;
    private bool playerInSight = false;
    private Vector3 rayDirection;
    private bool isRaycasting;
    public bool wasProvoked = false;

    [Header("Footstep Sounds")]
    public AudioClip[] walkFootsteps;
    public AudioClip[] runFootsteps;
    public float footstepVolume = 1f;

    [Header("SFX")]
    public AudioClip hurtSFX;
    public AudioClip deathSFX;

    [Header("Shield Settings")]
    [SerializeField] float currentShield;
    [SerializeField] float currentHP;
    [SerializeField] float shieldAmount = 1000000f;
    private bool shieldActivated = false;

    private EnemyHealth enemyHealth;
    private Animator animator;
    private float distanceToPlayer;
    private Vector3 lastPlayerPos;
    private Vector3 lastEnemyPos;
    private Collider enemyCollider;

    private void Awake()
    {
        // Component Reference
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        enemyHealth = GetComponent<EnemyHealth>();
        enemyCollider = GetComponent<Collider>();

        agent.updateRotation = true;
        agent.updateUpAxis = true;
    }

    void Start()
    {
        // Set awal
        lastPlayerPos = player.position;
        lastEnemyPos = transform.position;

        // Pastikan shield 0 saat mulai
        enemyHealth.shield = 0;
        currentShield = 0;

        agent.speed = walkSpeed;
        agent.angularSpeed = 720f;
        agent.acceleration = 16f;

        if (waypoints.Length > 0)
            agent.SetDestination(waypoints[currentWaypointIndex].position);
    }

    void Update()
    {
        currentHP = enemyHealth.health;
        currentShield = enemyHealth.shield;
        isRaycasting = false;
        playerInSight = false;

        HandleShield(); // Cek jika darah < 100 dan aktifkan shield

        if (player == null)
        {
            Wander();
            return;
        }

        // Deteksi player
        Vector3 directionToPlayer = (player.position - transform.position).normalized;
        distanceToPlayer = Vector3.Distance(transform.position, player.position);
        float angleToPlayer = Vector3.Angle(transform.forward, directionToPlayer);

        Vector3 rayOrigin = transform.position + Vector3.up * 0.5f;
        Vector3 rayTarget = player.position + Vector3.up * 0.5f;
        rayDirection = (rayTarget - rayOrigin).normalized;
        isRaycasting = true;

        if (distanceToPlayer <= detectionRange && angleToPlayer <= fieldOfView / 2)
        {
            RaycastHit[] hits = Physics.RaycastAll(rayOrigin, rayDirection, detectionRange);
            foreach (RaycastHit hit in hits)
            {
                if (((1 << hit.collider.gameObject.layer) & obstacleLayerMask) != 0)
                    break;

                if (hit.transform.CompareTag("Player"))
                {
                    playerInSight = true;
                    break;
                }
            }
        }

        if (playerInSight)
        {
            wasProvoked = true;
            HandleChaseOrAttack(distanceToPlayer);
        }
        else if (wasProvoked)
        {
            HandleChaseOrAttack(distanceToPlayer);
        }
        else
        {
            Wander();
        }

        lastPlayerPos = player.position;
        lastEnemyPos = transform.position;
    }

    void HandleShield()
    {
        if (!shieldActivated && currentHP <= 100)
        {
            enemyHealth.shield = shieldAmount;
            currentShield = shieldAmount;
            shieldActivated = true;
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

        SetAnimationState(true, false); // Jalan
    }

    void HandleChaseOrAttack(float distance)
    {
        if (distance > attackRange)
        {
            agent.isStopped = false;
            agent.speed = chaseSpeed;
            agent.SetDestination(player.position);

            isAttacking = false;
            SetAnimationState(true, false); // Tetap jalan
        }
        else
        {
            agent.isStopped = true;
            agent.speed = 0f;

            SetAnimationState(false, false);

            if (Time.time - lastAttackTime >= attackCooldown)
            {
                lastAttackTime = Time.time;
                isAttacking = true;
                animator.SetTrigger("attack");
            }
        }
    }

    public void DealMeleeDamage()
    {
        if (!isAttacking) return;
        if (enemyHealth.shield > 0) return; // Tidak bisa diserang saat shield aktif

        Collider[] hits = Physics.OverlapSphere(transform.position, attackRange + 0.5f, playerLayer);
        foreach (var hit in hits)
        {
            if (hit.CompareTag("Player"))
            {
                var stats = hit.GetComponent<PlayerStats>();
                if (stats != null)
                {
                    stats.Damage(meleeDamage, false);
                }
            }
        }
    }

    public void OnTakeDamage()
    {
        wasProvoked = true;
        SoundManager.Instance.PlaySound(hurtSFX, 0f, 0f, true, 1f);
    }

    public void DieTrigger()
    {
        if (agent != null)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
        }

        animator.SetBool("isWalking", false);
        animator.SetBool("isIdle", false);
        animator.ResetTrigger("attack");
        animator.SetTrigger("die");
        animator.SetBool("isDie", true);

        this.enabled = false;

        if (enemyCollider != null)
        {
            enemyCollider.isTrigger = true;
        }

        if (deathSFX != null)
        {
            SoundManager.Instance.PlaySound(deathSFX, 0f, 0f, true, 1f);
        }
    }

    public void Die()
    {
        Destroy(gameObject, 2f);
    }

    private void SetAnimationState(bool isWalking, bool isIdle)
    {
        animator.SetBool("isWalking", isWalking);
        animator.SetBool("isIdle", isIdle);
    }

    void OnDrawGizmos()
    {
        if (isRaycasting)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(transform.position + Vector3.up * 0.5f, rayDirection * detectionRange);
        }

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
