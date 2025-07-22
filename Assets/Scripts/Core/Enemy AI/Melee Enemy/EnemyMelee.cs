using System.Collections;
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
    private float baseMeleeDamage;

    [Header("State Flags")]
    private bool isAttacking = false;
    private bool playerInSight = false;
    private Vector3 rayDirection;
    private bool isRaycasting;
    private bool isRage = false;
    private bool rageAnimating = false;

    [Header("Combat Awareness")]
    public bool wasProvoked = false;

    [Header("Footstep Sounds")]
    public AudioClip[] walkFootsteps;
    public AudioClip[] runFootsteps;
    public float footstepVolume = 1f;

    [Header("SFX Settings")]
    public AudioClip hurtSFX;
    [SerializeField] AudioClip deathSFX;
    [SerializeField] AudioClip rageSFX;

    [Header("Rage Settings")]
    public float rageThreshold = 60f;
    public float rageSpeedBoost = 2f;
    public float rageDamageMultiplier = 1.5f;

    [Header("Shield Settings")]
    [SerializeField] float shield;
    [SerializeField] public float currentShield;
    private bool shield250 = false, shield150 = false, shield80 = false;
    private float shieldTimer = 0f;
    [SerializeField] float regenRate = 10f;
    private float lastShieldValue;
    private bool shieldDamaged = false;
    private float maxShield = 0f;

    private Vector3 lastPlayerPos, lastEnemyPos;
    private EnemyHealth enemyHealth;
    private Animator animator;
    private Collider enemyCollider;
    private float distanceToPlayer;
    [SerializeField] private float currentHealth;

    private void Awake()
    {
        enemyHealth = GetComponent<EnemyHealth>();
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        enemyCollider = GetComponent<Collider>();

        agent.updateRotation = true;
        agent.updateUpAxis = true;
    }

    void Start()
    {
        currentHealth = enemyHealth.health;
        enemyHealth.shield = 0;
        currentShield = 0;
        lastShieldValue = 0;

        agent.speed = walkSpeed;
        agent.angularSpeed = 720f;
        agent.acceleration = 16f;

        baseMeleeDamage = meleeDamage;

        lastPlayerPos = player.position;
        lastEnemyPos = transform.position;

        if (waypoints.Length > 0)
            agent.SetDestination(waypoints[currentWaypointIndex].position);
    }

    void Update()
    {
        currentHealth = enemyHealth.health;
        currentShield = enemyHealth.shield;
        isRaycasting = false;

        HandleShield();

        if (!isRage && currentHealth <= rageThreshold && !rageAnimating)
            StartCoroutine(Rage());

        if (player == null || rageAnimating)
        {
            if (!rageAnimating) Wander();
            return;
        }

        Vector3 directionToPlayer = (player.position - transform.position).normalized;
        distanceToPlayer = Vector3.Distance(transform.position, player.position);
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
                    break;

                float d = hit.distance;
                if (d < closestDist)
                {
                    closestDist = d;
                    closest = hit.transform;
                }
            }

            if (closest != null && closest.CompareTag("Player"))
                playerInSight = true;
        }

        if (playerInSight || wasProvoked)
            HandleChaseOrAttack(distanceToPlayer);
        else
            Wander();

        lastPlayerPos = player.position;
        lastEnemyPos = transform.position;
    }

    void HandleShield()
    {
        // Aktifkan shield sekali berdasarkan fase HP
        if (!shield250 && currentHealth <= 250)
        {
            enemyHealth.shield = 100;
            maxShield = 100;
            shield250 = true;
        }
        else if (!shield150 && currentHealth <= 150)
        {
            enemyHealth.shield = 100;
            maxShield = 100;
            shield150 = true;
        }
        else if (!shield80 && currentHealth <= 80)
        {
            enemyHealth.shield = 100;
            maxShield = 100;
            shield80 = true;
        }

        if (enemyHealth.shield > 0)
        {
            bool playerStationary = Vector3.Distance(player.position, lastPlayerPos) < 0.01f;
            bool enemyStationary = Vector3.Distance(transform.position, lastEnemyPos) < 0.01f;

            // Deteksi apakah shield berkurang karena serangan
            if (enemyHealth.shield < lastShieldValue)
                shieldDamaged = true;

            lastShieldValue = enemyHealth.shield;

            if (enemyHealth.shield < maxShield)
            {
                enemyHealth.shield += regenRate * Time.deltaTime;
                if (enemyHealth.shield > maxShield)
                {
                    enemyHealth.shield = maxShield;
                    shieldDamaged = false;
                }
            }

            if (playerStationary && enemyStationary && !shieldDamaged)
            {
                shieldTimer += Time.deltaTime;
                if (shieldTimer >= 5f)
                {
                    enemyHealth.shield = 0;
                    shieldTimer = 0f;
                    maxShield = 0f;
                    shieldDamaged = false;
                }

                agent.isStopped = true;
                SetIdleState();
            }
            else
            {
                shieldTimer = 0f;
                agent.isStopped = false;
                animator.SetBool("isIdle", false);
                animator.SetBool("isRunning", true);
                animator.SetBool("isWalking", false);
            }
        }
    }

    void HandleChaseOrAttack(float distance)
    {
        // Jika shield aktif dan player tidak bergerak, idle
        if (enemyHealth.shield > 0 && Vector3.Distance(player.position, lastPlayerPos) < 0.01f)
        {
            agent.isStopped = true;
            SetIdleState();
            return;
        }

        // Jika musuh berada dalam jarak serang
        if (distance <= attackRange)
        {
            agent.isStopped = true;
            agent.ResetPath();
            agent.velocity = Vector3.zero;

            // Matikan semua animasi gerakan
            animator.SetBool("isRunning", false);
            animator.SetBool("isWalking", false);
            animator.SetBool("isIdle", false);
            animator.SetBool("isRage", isRage);

            // Lakukan serangan jika cooldown sudah selesai
            if (Time.time - lastAttackTime >= attackCooldown)
            {
                lastAttackTime = Time.time;
                isAttacking = true;
                animator.SetTrigger("attack");
            }
        }
        // Jika musuh di luar jangkauan serang
        else
        {
            agent.isStopped = false;
            agent.speed = isRage ? chaseSpeed + rageSpeedBoost : chaseSpeed;
            agent.SetDestination(player.position);

            isAttacking = false;

            // Hanya aktifkan isRunning jika agent benar-benar bergerak
            if (agent.velocity.magnitude > 0.1f)
            {
                animator.SetBool("isRunning", true);
                animator.SetBool("isWalking", false);
                animator.SetBool("isIdle", false);
            }
            else
            {
                SetIdleState();
            }

            animator.SetBool("isRage", isRage);
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
        animator.SetBool("isIdle", false);
    }

    IEnumerator Rage()
    {
        isRage = true;
        rageAnimating = true;

        agent.isStopped = true;
        agent.velocity = Vector3.zero;
        agent.ResetPath();

        animator.SetTrigger("rage");
        animator.SetBool("isWalking", false);
        animator.SetBool("isRunning", false);
        animator.SetBool("isRage", true);

        yield return new WaitUntil(() => animator.GetCurrentAnimatorStateInfo(0).IsName("rage"));
        yield return new WaitUntil(() => animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 0.98f);

        rageAnimating = false;
        agent.isStopped = false;

        wasProvoked = true;
        agent.speed = chaseSpeed + rageSpeedBoost;
        meleeDamage = baseMeleeDamage * rageDamageMultiplier;
    }

    public void RageSFX()
    {
        //if (rageSFX != null)
            SoundManager.Instance.PlaySound(rageSFX, 0f, .01f, false, 1f);
    }

    public void DisableRage()
    {
        isRage = true;
        rageAnimating = false;
        wasProvoked = true;

        agent.isStopped = false;
        agent.speed = chaseSpeed + rageSpeedBoost;

        animator.ResetTrigger("rage");
        animator.SetBool("isRage", true);
        animator.SetBool("isRunning", true);
        animator.SetBool("isIdle", false);

        if (player != null)
            agent.SetDestination(player.position);
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
                    stats.Damage(meleeDamage, false);
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
            agent.ResetPath();
        }

        animator.SetBool("isRunning", false);
        animator.SetBool("isWalking", false);
        animator.SetBool("isIdle", false);
        animator.SetBool("isRage", false);
        animator.ResetTrigger("attack");
        animator.SetTrigger("die");
        animator.SetBool("isDie", true);

        this.enabled = false;

        if (enemyCollider != null)
            enemyCollider.isTrigger = true;

        if (deathSFX != null)
            SoundManager.Instance.PlaySound(deathSFX, 0f, 0f, true, 1f);
    }

    public void Die()
    {
        Destroy(gameObject, 2f);
    }

    public void PlayFootstepSound()
    {
        AudioClip[] selectedClips = animator.GetBool("isRunning") ? runFootsteps : walkFootsteps;
        if (selectedClips.Length == 0) return;

        int index = Random.Range(0, selectedClips.Length);
        AudioClip clip = selectedClips[index];
        SoundManager.Instance.PlaySound(clip, 0f, 0f, true, 0f, footstepVolume);
    }

    private void SetIdleState()
    {
        animator.SetBool("isIdle", true);
        animator.SetBool("isRunning", false);
        animator.SetBool("isWalking", false);
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
}
