using UnityEngine;
using UnityEngine.AI;
using cowsins;
using System.Collections;

public class Pulzar : MonoBehaviour
{
    // State Machine Enum untuk kejelasan
    private enum AIState { Wandering, Chasing, Attacking, Raging, VulnerableShield, IndestructibleShield, Dead }
    private AIState currentState;

    [Header("Waypoint Settings")]
    public Transform[] waypoints;
    private int currentWaypointIndex = 0;

    [Tooltip("Waypoint tujuan saat HP musuh mencapai 100 (fase perisai terakhir).")]
    public Transform finalPhaseWaypoint;

    [Header("Navigation & Movement Feel")]
    private NavMeshAgent agent;
    public float walkSpeed = 2f;
    public float chaseSpeed = 4f;
    [Tooltip("Seberapa cepat enemy berputar. Nilai rendah membuat putaran lebih lebar & natural.")]
    public float angularSpeed = 240f;
    [Tooltip("Seberapa cepat enemy mencapai kecepatan penuh. Nilai rendah lebih mulus.")]
    public float acceleration = 12f;

    [Header("Player Detection")]
    public Transform player;
    public LayerMask obstacleLayerMask;
    public float fieldOfView = 120f;
    public float detectionRange = 15f;
    public float attackRange = 2f;

    [Header("Attack Settings")]
    public float meleeDamage = 20f;
    public float attackCooldown = 2f;
    private float lastAttackTime;

    [Header("Combat Awareness")]
    private bool wasProvoked = false;

    [Header("SFX")]
    public AudioClip[] attackSFX;
    [SerializeField] AudioClip rageSFX;
    public AudioClip hurtSFX;
    public AudioClip deathSFX;
    public float sfxVolume = 1f;

    [Header("Shield Settings")]
    public GameObject shieldVFX;
    public float shieldAmount = 99999f;
    private bool shieldTriggeredAt250, shieldTriggeredAt200, shieldTriggeredAt150, indestructibleShieldTriggered;
    private float shieldBreakTimer = 0f;

    [Header("Stats")]
    [SerializeField] float currentHealth;
    [SerializeField] float currentShield;

    private EnemyHealth enemyHealth;
    private Animator animator;
    private Vector3 lastPlayerPos;
    private Vector3 lastEnemyPos;
    private Collider enemyCollider;
    private AudioSource audioSource;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        enemyHealth = GetComponent<EnemyHealth>();
        enemyCollider = GetComponent<Collider>();
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.spatialBlend = 1f;
        audioSource.playOnAwake = false;
        audioSource.volume = sfxVolume;
    }

    void Start()
    {
        currentState = AIState.Wandering;
        enemyHealth.shield = 0;
        if (shieldVFX != null) shieldVFX.SetActive(false);

        agent.speed = walkSpeed;
        agent.angularSpeed = angularSpeed;
        agent.acceleration = acceleration;

        if (waypoints.Length > 0) agent.SetDestination(waypoints[currentWaypointIndex].position);
    }

    void Update()
    {
        currentHealth = enemyHealth.health;
        currentShield = enemyHealth.shield;

        if (player == null || currentState == AIState.Dead) return;

        if (currentState != AIState.VulnerableShield)
        {
            CheckShieldTriggers();
        }

        switch (currentState)
        {
            case AIState.VulnerableShield:
                HandleVulnerableShield();
                break;
            case AIState.IndestructibleShield:
                HandleIndestructibleShield();
                break;
            case AIState.Raging:
                break;
            case AIState.Wandering:
                Wander();
                DetectPlayer();
                break;
            case AIState.Chasing:
                Chase();
                break;
            case AIState.Attacking:
                Attack();
                break;
        }

        lastPlayerPos = player.position;
        lastEnemyPos = transform.position;
    }

    void CheckShieldTriggers()
    {
        float currentHP = enemyHealth.health;
        if (currentHP <= 250 && !shieldTriggeredAt250) ActivateVulnerableShield(ref shieldTriggeredAt250);
        else if (currentHP <= 200 && !shieldTriggeredAt200) ActivateVulnerableShield(ref shieldTriggeredAt200);
        else if (currentHP <= 150 && !shieldTriggeredAt150) ActivateVulnerableShield(ref shieldTriggeredAt150);
        else if (currentHP <= 100 && !indestructibleShieldTriggered) ActivateIndestructibleShield();
    }

    void ActivateVulnerableShield(ref bool triggerFlag)
    {
        triggerFlag = true;
        currentState = AIState.VulnerableShield;
        enemyHealth.shield = shieldAmount;
        if (shieldVFX != null) shieldVFX.SetActive(true);
        agent.isStopped = true;
        agent.velocity = Vector3.zero;
        SetAnimationState(false, true);
        shieldBreakTimer = 0f;
        Debug.Log("Vulnerable Shield Activated!");
    }

    void ActivateIndestructibleShield()
    {
        indestructibleShieldTriggered = true;
        currentState = AIState.IndestructibleShield;
        enemyHealth.shield = shieldAmount;
        if (shieldVFX != null) shieldVFX.SetActive(true);
        if (finalPhaseWaypoint != null)
        {
            agent.isStopped = false;
            agent.speed = walkSpeed;
            agent.SetDestination(finalPhaseWaypoint.position);
            SetAnimationState(true, false);
        }
        else
        {
            agent.isStopped = true;
            SetAnimationState(false, true);
        }
        Debug.Log("Indestructible Shield Activated!");
    }

    void HandleIndestructibleShield()
    {
        if (finalPhaseWaypoint == null)
        {
            agent.isStopped = true;
            SetAnimationState(false, true);
            return;
        }
        if (!agent.pathPending && agent.remainingDistance < 0.5f)
        {
            agent.isStopped = true;
            SetAnimationState(false, true);
        }
        else
        {
            SetAnimationState(true, false);
        }
    }

    void HandleVulnerableShield()
    {
        bool playerIsMoving = Vector3.Distance(player.position, lastPlayerPos) > 0.01f;
        if (playerIsMoving)
        {
            float distanceToPlayer = Vector3.Distance(transform.position, player.position);
            if (distanceToPlayer > attackRange)
            {
                agent.isStopped = false;
                agent.speed = chaseSpeed;
                agent.SetDestination(player.position);
                SetAnimationState(true, false);
            }
            else
            {
                agent.isStopped = true;
                SetAnimationState(false, true);
            }
            shieldBreakTimer = 0f;
        }
        else
        {
            agent.isStopped = true;
            SetAnimationState(false, true);
            if (agent.velocity.magnitude < 0.1f)
            {
                shieldBreakTimer += Time.deltaTime;
                if (shieldBreakTimer >= 5f)
                {
                    Debug.Log("Shield dihancurkan setelah 5 detik diam!");
                    enemyHealth.shield = 0;
                    if (shieldVFX != null) shieldVFX.SetActive(false);
                    shieldBreakTimer = 0f;
                    currentState = AIState.Chasing;
                    agent.isStopped = false;
                }
            }
            else
            {
                shieldBreakTimer = 0f;
            }
        }
    }

    void DetectPlayer()
    {
        if (player == null) return;
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        if (distanceToPlayer > detectionRange) return;
        Vector3 directionToPlayer = (player.position - transform.position).normalized;
        if (Vector3.Angle(transform.forward, directionToPlayer) > fieldOfView / 2) return;
        Vector3 rayOrigin = transform.position + Vector3.up * 0.5f;
        if (!Physics.Raycast(rayOrigin, directionToPlayer, distanceToPlayer, obstacleLayerMask))
        {
            if (!wasProvoked)
            {
                wasProvoked = true;
                StartCoroutine(TriggerRage());
            }
        }
    }

    IEnumerator TriggerRage()
    {
        currentState = AIState.Raging;
        agent.isStopped = true;
        ResetAllAnimationStates();
        animator.SetBool("isRage", true);
        PlayRageSFX();
        yield return new WaitForSeconds(2.05f);
        animator.SetBool("isRage", false);
        currentState = AIState.Chasing;
        agent.isStopped = false;
    }

    void Wander()
    {
        agent.speed = walkSpeed;
        if (waypoints.Length == 0)
        {
            agent.isStopped = true;
            SetAnimationState(false, true);
            return;
        }
        if (!agent.pathPending && agent.remainingDistance < 0.5f)
        {
            currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Length;
            agent.SetDestination(waypoints[currentWaypointIndex].position);
        }
        SetAnimationState(true, false);
    }

    void Chase()
    {
        agent.speed = chaseSpeed;
        float distance = Vector3.Distance(transform.position, player.position);
        if (distance <= attackRange)
        {
            currentState = AIState.Attacking;
        }
        else
        {
            agent.isStopped = false;
            agent.SetDestination(player.position);
            SetAnimationState(true, false);
        }
    }

    void Attack()
    {
        agent.isStopped = true;
        Vector3 direction = (player.position - transform.position).normalized;
        Quaternion lookRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 5f);
        SetAnimationState(false, false);
        if (Time.time - lastAttackTime >= attackCooldown)
        {
            lastAttackTime = Time.time;
            animator.SetTrigger("attack");
            PlayAttackSFX();
        }
        if (Vector3.Distance(transform.position, player.position) > attackRange)
        {
            currentState = AIState.Chasing;
        }
    }

    public void OnTakeDamage()
    {
        if (currentState == AIState.Wandering && !wasProvoked)
        {
            wasProvoked = true;
            StartCoroutine(TriggerRage());
        }
        if (hurtSFX != null && audioSource != null) audioSource.PlayOneShot(hurtSFX);
    }

    public void Die()
    {
        if (currentState == AIState.IndestructibleShield)
        {
            Debug.Log("Perisai terakhir dihancurkan secara paksa!");
            enemyHealth.shield = 0;
            if (shieldVFX != null) shieldVFX.SetActive(false);
        }
        DieTrigger();
    }

    private void DieTrigger()
    {
        currentState = AIState.Dead;
        if (agent != null) agent.isStopped = true;
        ResetAllAnimationStates();
        animator.SetBool("isDie", true);
        if (deathSFX != null && audioSource != null) audioSource.PlayOneShot(deathSFX);
        if (enemyCollider != null) enemyCollider.enabled = false;
        Destroy(gameObject, 3f);
    }

    // --- FUNGSI DIPERBAIKI ---
    public void DealMeleeDamage()
    {
        // Pengecekan awal, jangan lakukan apapun jika tidak sedang menyerang atau shield aktif
        if (currentState != AIState.Attacking || enemyHealth.shield > 0) return;

        // Cek jarak sekali lagi untuk memastikan player masih dalam jangkauan
        if (Vector3.Distance(transform.position, player.position) <= attackRange)
        {
            // Coba dapatkan komponen PlayerStats dari player
            PlayerStats playerStats = player.GetComponent<PlayerStats>();
            if (playerStats != null)
            {
                // Berikan damage ke player
                playerStats.Damage(meleeDamage, false);
                Debug.Log("Player terkena serangan melee sebesar " + meleeDamage + " damage!");
            }
        }
    }

    public void PlayRageSFX() { if (rageSFX != null) audioSource.PlayOneShot(rageSFX); }
    public void PlayAttackSFX()
    {
        if (attackSFX.Length == 0) return;
        AudioClip clip = attackSFX[Random.Range(0, attackSFX.Length)];
        audioSource.PlayOneShot(clip, sfxVolume);
    }

    private void SetAnimationState(bool isWalking, bool isIdle)
    {
        animator.SetBool("isWalking", isWalking);
        animator.SetBool("isIdle", isIdle);
    }

    private void ResetAllAnimationStates()
    {
        SetAnimationState(false, false);
        animator.SetBool("isRage", false);
        animator.ResetTrigger("attack");
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        if (player != null)
        {
            Vector3 fovLine1 = Quaternion.AngleAxis(fieldOfView / 2, transform.up) * transform.forward * detectionRange;
            Vector3 fovLine2 = Quaternion.AngleAxis(-fieldOfView / 2, transform.up) * transform.forward * detectionRange;
            Gizmos.color = Color.cyan;
            Gizmos.DrawRay(transform.position, fovLine1);
            Gizmos.DrawRay(transform.position, fovLine2);
        }
    }
}