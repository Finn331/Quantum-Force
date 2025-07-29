using UnityEngine;
using UnityEngine.AI;
using cowsins;
using System.Collections;

public class Singulra : MonoBehaviour
{
    // State Machine Enum
    private enum AIState { Wandering, Chasing, Attacking, Raging, Stunned, Phase1_VulnerableShield, Phase2_WaypointShield, Phase3_WreckingBallShield, Dead }
    private AIState currentState;

    [Header("Core Components")]
    private NavMeshAgent agent;
    private Animator animator;
    private EnemyHealth enemyHealth;
    private AudioSource audioSource;
    private Collider enemyCollider;
    public Transform player;

    [Header("Phase Settings (Health Thresholds)")]
    public float maxHealth = 1500f;
    private const float phase1_Threshold = 1250f;
    private const float phase2a_Threshold = 1000f;
    private const float phase2b_Threshold = 750f;
    private const float phase3_Threshold = 500f;
    private bool phase1Triggered, phase2aTriggered, phase2bTriggered, phase3Triggered;

    [Header("Wandering & Phase Waypoints")]
    public Transform[] wanderWaypoints;
    private int currentWanderWaypointIndex = 0;
    public Transform phase2a_Waypoint;
    public Transform phase2b_Waypoint;

    [Header("Navigation & Movement Feel")]
    public float walkSpeed = 2f;
    public float chaseSpeed = 4f;
    public float angularSpeed = 240f;
    public float acceleration = 12f;

    [Header("Player Detection")]
    public LayerMask visionBlockLayer;
    public float detectionRange = 20f;
    public float attackRange = 3f;
    [Range(0, 360)]
    public float fieldOfViewAngle = 120f;
    public float eyeHeight = 1.5f;

    [Header("Stun Settings")]
    public float stunDuration = 3f;

    [Header("Attack Settings")]
    public float meleeDamage = 20f;
    public float attackCooldown = 2f;
    private float lastAttackTime;
    private bool wasProvoked = false;

    [Header("Rage Mode Settings")]
    private bool isEnraged = false;
    public float rageChaseSpeed = 6f;
    public float rageMeleeDamage = 30f;
    private const float ENRAGE_THRESHOLD = 0.9f;

    [Header("SFX")]
    public AudioClip[] attackSFX;
    [SerializeField] AudioClip rageSFX;
    public AudioClip hurtSFX;
    public AudioClip deathSFX;
    public AudioClip shieldUpSFX;
    public AudioClip shieldDownSFX;
    public float sfxVolume = 1f;

    [Header("Shield Mechanics")]
    public GameObject shieldVFX;
    public float shieldAmount = 99999f;
    private float shieldBreakTimer = 0f;
    private const float SHIELD_BREAK_DELAY = 5f;

    private Vector3 lastPlayerPos;

    #region Unity Lifecycle & Setup
    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        enemyHealth = GetComponent<EnemyHealth>();
        enemyCollider = GetComponent<Collider>();
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
    }

    void Start()
    {
        currentState = AIState.Wandering;
        enemyHealth.health = maxHealth;
        enemyHealth.shield = 0;
        if (shieldVFX != null) shieldVFX.SetActive(false);
        audioSource.spatialBlend = 1f;
        audioSource.playOnAwake = false;
        audioSource.volume = sfxVolume;
        agent.speed = walkSpeed;
        agent.angularSpeed = angularSpeed;
        agent.acceleration = acceleration;
        agent.stoppingDistance = attackRange;

        if (wanderWaypoints.Length > 0)
        {
            agent.SetDestination(wanderWaypoints[currentWanderWaypointIndex].position);
        }
    }

    void Update()
    {
        if (player == null || currentState == AIState.Dead) return;

        if (currentState == AIState.Wandering || currentState == AIState.Chasing || currentState == AIState.Attacking)
        {
            CheckPhaseTriggers();
        }

        switch (currentState)
        {
            case AIState.Stunned: break;
            case AIState.Phase1_VulnerableShield: HandlePhase1Shield(); break;
            case AIState.Phase2_WaypointShield: HandlePhase2Shield(); break;
            case AIState.Phase3_WreckingBallShield: HandlePhase3Shield(); break;
            case AIState.Wandering: Wander(); DetectPlayer(); break;
            case AIState.Chasing: Chase(); break;
            case AIState.Attacking: Attack(); break;
        }

        lastPlayerPos = player.position;
    }
    #endregion

    #region Stun Collision
    private void OnTriggerEnter(Collider other)
    {
        //if (other.CompareTag("FallenObject") && currentState != AIState.Dead && currentState != AIState.Stunned)
        //{
        //    StartCoroutine(GetStunned());
        //}

        if (other.gameObject.CompareTag("FallenObject") && currentState != AIState.Dead && currentState != AIState.Stunned)
        {
            // Jika terkena objek jatuh, langsung masuk ke state Stunned
            StartCoroutine(GetStunned());
        }

    }


    // --- FUNGSI STUN DIPERBAIKI ---
    IEnumerator GetStunned()
    {
        Debug.Log("Singulra terkena STUN!");
        AIState stateBeforeStun = currentState;
        currentState = AIState.Stunned;

        agent.isStopped = true;
        agent.velocity = Vector3.zero;

        // Matikan semua animasi gerak lalu picu animasi stun
        ResetAllAnimationStates();
        animator.SetTrigger("stunned");

        yield return new WaitForSeconds(stunDuration);

        Debug.Log("Stun selesai, kembali ke state sebelumnya.");
        currentState = stateBeforeStun;
        if (currentState != AIState.Dead) agent.isStopped = false;
    }
    #endregion

    #region Phase Mechanics
    void CheckPhaseTriggers()
    {
        float currentHP = enemyHealth.health;

        if (!isEnraged && currentHP <= maxHealth * ENRAGE_THRESHOLD)
        {
            ActivateEnrageMode();
        }

        if (currentHP <= phase3_Threshold && !phase3Triggered) StartCoroutine(EnterPhase(AIState.Phase3_WreckingBallShield));
        else if (currentHP <= phase2b_Threshold && !phase2bTriggered) StartCoroutine(EnterPhase(AIState.Phase2_WaypointShield, phase2b_Waypoint, "2b"));
        else if (currentHP <= phase2a_Threshold && !phase2aTriggered) StartCoroutine(EnterPhase(AIState.Phase2_WaypointShield, phase2a_Waypoint, "2a"));
        else if (currentHP <= phase1_Threshold && !phase1Triggered) StartCoroutine(EnterPhase(AIState.Phase1_VulnerableShield));
    }

    void ActivateEnrageMode()
    {
        isEnraged = true;
        chaseSpeed = rageChaseSpeed;
        meleeDamage = rageMeleeDamage;
        PlaySFX(rageSFX);
        Debug.Log("ENRAGE MODE ACTIVATED!");
    }

    IEnumerator EnterPhase(AIState nextPhase, Transform waypoint = null, string phaseIdentifier = "")
    {
        currentState = AIState.Raging;
        agent.isStopped = true;
        ResetAllAnimationStates();
        animator.SetBool("isRage", true);
        PlaySFX(rageSFX);
        yield return new WaitForSeconds(2.0f);
        animator.SetBool("isRage", false);
        switch (nextPhase)
        {
            case AIState.Phase1_VulnerableShield: ActivatePhase1Shield(); break;
            case AIState.Phase2_WaypointShield:
                if (phaseIdentifier == "2a") ActivatePhase2Shield(waypoint, ref phase2aTriggered);
                else if (phaseIdentifier == "2b") ActivatePhase2Shield(waypoint, ref phase2bTriggered);
                break;
            case AIState.Phase3_WreckingBallShield: ActivatePhase3Shield(); break;
        }
    }

    void ActivatePhase1Shield()
    {
        phase1Triggered = true;
        currentState = AIState.Phase1_VulnerableShield;
        ActivateShieldVisuals();
        agent.isStopped = true;
        SetAnimationState(false, false, true);
        shieldBreakTimer = 0f;
    }

    void HandlePhase1Shield()
    {
        bool playerIsMoving = Vector3.Distance(player.position, lastPlayerPos) > 0.01f;
        if (playerIsMoving)
        {
            agent.isStopped = false;
            agent.speed = chaseSpeed;
            agent.SetDestination(player.position);
            SetAnimationState(false, true, false);
            shieldBreakTimer = 0f;
        }
        else
        {
            agent.isStopped = true;
            SetAnimationState(false, false, true);
            if (agent.velocity.magnitude < 0.1f)
            {
                shieldBreakTimer += Time.deltaTime;
                if (shieldBreakTimer >= SHIELD_BREAK_DELAY)
                {
                    BreakShield();
                }
            }
        }
    }

    void ActivatePhase2Shield(Transform waypoint, ref bool triggerFlag)
    {
        triggerFlag = true;
        currentState = AIState.Phase2_WaypointShield;
        ActivateShieldVisuals();
        if (waypoint != null)
        {
            agent.isStopped = false;
            agent.speed = walkSpeed;
            agent.SetDestination(waypoint.position);
        }
        else
        {
            agent.isStopped = true;
        }
    }

    void HandlePhase2Shield()
    {
        if (!agent.pathPending && agent.remainingDistance < agent.stoppingDistance)
        {
            agent.isStopped = true;
            SetAnimationState(false, false, true);
        }
        else
        {
            SetAnimationState(true, false, false);
        }
    }

    void ActivatePhase3Shield()
    {
        phase3Triggered = true;
        currentState = AIState.Phase3_WreckingBallShield;
        ActivateShieldVisuals();
        agent.isStopped = true;
    }

    void HandlePhase3Shield()
    {
        agent.isStopped = true;
        SetAnimationState(false, false, true);
    }
    #endregion

    #region Public Event Functions
    public void BreakPhase2Shield() { if (currentState == AIState.Phase2_WaypointShield) BreakShield(); }
    public void BreakPhase3ShieldByWreckingBall() { if (currentState == AIState.Phase3_WreckingBallShield) BreakShield(); }
    #endregion

    #region Standard AI Behavior
    void DetectPlayer()
    {
        if (wasProvoked || player == null) return;
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        if (distanceToPlayer > detectionRange) return;

        Vector3 directionToPlayer = (player.position - transform.position).normalized;
        if (Vector3.Angle(transform.forward, directionToPlayer) < fieldOfViewAngle / 2)
        {
            Vector3 eyePosition = transform.position + Vector3.up * eyeHeight;
            if (!Physics.Raycast(eyePosition, directionToPlayer, distanceToPlayer, visionBlockLayer))
            {
                wasProvoked = true;
                StartCoroutine(TriggerInitialRage());
            }
        }
    }

    IEnumerator TriggerInitialRage()
    {
        currentState = AIState.Raging;
        agent.isStopped = true;
        ResetAllAnimationStates();
        animator.SetBool("isRage", true);
        PlaySFX(rageSFX);
        yield return new WaitForSeconds(2.0f);
        animator.SetBool("isRage", false);
        currentState = AIState.Chasing;
        agent.isStopped = false;
    }

    void Wander()
    {
        agent.speed = walkSpeed;
        if (wanderWaypoints.Length == 0)
        {
            agent.isStopped = true;
            SetAnimationState(false, false, true);
            return;
        }
        if (!agent.pathPending && agent.remainingDistance < agent.stoppingDistance)
        {
            currentWanderWaypointIndex = (currentWanderWaypointIndex + 1) % wanderWaypoints.Length;
            agent.SetDestination(wanderWaypoints[currentWanderWaypointIndex].position);
        }
        SetAnimationState(true, false, false);
    }

    void Chase()
    {
        agent.speed = chaseSpeed;
        agent.isStopped = false;
        agent.SetDestination(player.position);
        SetAnimationState(false, true, false);

        if (Vector3.Distance(transform.position, player.position) <= agent.stoppingDistance)
        {
            currentState = AIState.Attacking;
        }
    }

    void Attack()
    {
        agent.isStopped = true;
        Vector3 direction = (player.position - transform.position).normalized;
        Quaternion lookRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 5f);
        SetAnimationState(false, false, false);

        if (Time.time - lastAttackTime >= attackCooldown)
        {
            lastAttackTime = Time.time;

            if (isEnraged)
            {
                int attackChoice = Random.Range(0, 2);
                if (attackChoice == 0)
                {
                    animator.SetTrigger("attack");
                }
                else
                {
                    animator.SetTrigger("attack2");
                }
            }
            else
            {
                animator.SetTrigger("attack");
            }
        }

        if (Vector3.Distance(transform.position, player.position) > agent.stoppingDistance)
        {
            currentState = AIState.Chasing;
        }
    }
    #endregion

    #region Damage, Death & SFX
    public void OnTakeDamage()
    {
        if (currentState == AIState.Wandering && !wasProvoked)
        {
            wasProvoked = true;
            StartCoroutine(TriggerInitialRage());
        }
        PlaySFX(hurtSFX);
    }

    public void Die()
    {
        if (currentState == AIState.Phase2_WaypointShield || currentState == AIState.Phase3_WreckingBallShield)
        {
            BreakShield();
        }
        DieTrigger();
    }

    private void DieTrigger()
    {
        currentState = AIState.Dead;
        agent.isStopped = true;
        ResetAllAnimationStates();
        animator.SetBool("isDie", true);
        PlaySFX(deathSFX);
        if (enemyCollider != null) enemyCollider.enabled = false;
        Destroy(gameObject, 3f);
    }

    public void DealMeleeDamage()
    {
        if (currentState != AIState.Attacking || enemyHealth.shield > 0) return;
        if (Vector3.Distance(transform.position, player.position) <= attackRange)
        {
            Debug.Log("Player terkena serangan melee sebesar " + meleeDamage + " damage!");
        }
    }

    public void PlayRageSFX() { PlaySFX(rageSFX); }
    public void PlayAttackSFX()
    {
        if (attackSFX.Length == 0) return;
        AudioClip clip = attackSFX[Random.Range(0, attackSFX.Length)];
        PlaySFX(clip);
    }

    private void PlaySFX(AudioClip clip)
    {
        if (clip != null && audioSource != null) audioSource.PlayOneShot(clip, sfxVolume);
    }
    #endregion

    #region Utility & Animation
    void ActivateShieldVisuals()
    {
        enemyHealth.shield = shieldAmount;
        if (shieldVFX != null) shieldVFX.SetActive(true);
        PlaySFX(shieldUpSFX);
    }

    void BreakShield()
    {
        enemyHealth.shield = 0;
        if (shieldVFX != null) shieldVFX.SetActive(false);
        PlaySFX(shieldDownSFX);
        currentState = AIState.Chasing;
        agent.isStopped = false;
    }

    private void SetAnimationState(bool isWalking, bool isRunning, bool isIdle)
    {
        animator.SetBool("isWalking", isWalking);
        animator.SetBool("isRunning", isRunning);
        animator.SetBool("isIdle", isIdle);
    }

    private void ResetAllAnimationStates()
    {
        SetAnimationState(false, false, false);
        animator.SetBool("isRage", false);
        animator.ResetTrigger("attack");
        animator.ResetTrigger("attack2");
        animator.ResetTrigger("stunned"); // Reset trigger stun untuk jaga-jaga
    }
    #endregion

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        Vector3 fovLine1 = Quaternion.AngleAxis(fieldOfViewAngle / 2, transform.up) * transform.forward * detectionRange;
        Vector3 fovLine2 = Quaternion.AngleAxis(-fieldOfViewAngle / 2, transform.up) * transform.forward * detectionRange;
        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(transform.position, fovLine1);
        Gizmos.DrawRay(transform.position, fovLine2);
    }
}