using UnityEngine;
using UnityEngine.AI;
using cowsins;
using System.Collections;

public class Singulra : MonoBehaviour
{
    // State Machine Enum untuk kejelasan
    private enum AIState { Wandering, Chasing, Attacking, Raging, Phase1_VulnerableShield, Phase2_WaypointShield, Phase3_WreckingBallShield, Dead }
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
    public LayerMask obstacleLayerMask;
    public float fieldOfView = 120f;
    public float detectionRange = 15f;
    public float attackRange = 2f;

    [Header("Attack Settings")]
    public float meleeDamage = 20f;
    public float attackCooldown = 2f;
    private float lastAttackTime;
    private bool wasProvoked = false;

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

    [Header("Stats (For Debugging)")]
    [SerializeField] float currentHealth;
    [SerializeField] float currentShield;

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

        // Setup Health & Shield
        enemyHealth.health = maxHealth;
        enemyHealth.shield = 0;
        if (shieldVFX != null) shieldVFX.SetActive(false);

        // Setup AudioSource
        audioSource.spatialBlend = 1f;
        audioSource.playOnAwake = false;
        audioSource.volume = sfxVolume;

        // Setup NavMeshAgent
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
        currentHealth = enemyHealth.health;
        currentShield = enemyHealth.shield;

        if (player == null || currentState == AIState.Dead) return;

        if (currentState != AIState.Phase1_VulnerableShield &&
            currentState != AIState.Phase2_WaypointShield &&
            currentState != AIState.Phase3_WreckingBallShield)
        {
            CheckPhaseTriggers();
        }

        switch (currentState)
        {
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

    #region Phase Mechanics
    void CheckPhaseTriggers()
    {
        float currentHP = enemyHealth.health;

        if (currentHP <= phase3_Threshold && !phase3Triggered) ActivatePhase3Shield();
        else if (currentHP <= phase2b_Threshold && !phase2bTriggered) ActivatePhase2Shield(phase2b_Waypoint, ref phase2bTriggered);
        else if (currentHP <= phase2a_Threshold && !phase2aTriggered) ActivatePhase2Shield(phase2a_Waypoint, ref phase2aTriggered);
        else if (currentHP <= phase1_Threshold && !phase1Triggered) ActivatePhase1Shield();
    }

    void ActivatePhase1Shield()
    {
        phase1Triggered = true;
        currentState = AIState.Phase1_VulnerableShield;
        ActivateShieldVisuals();
        agent.isStopped = true;
        SetAnimationState(false, true);
        shieldBreakTimer = 0f;
    }

    void HandlePhase1Shield()
    {
        bool playerIsMoving = Vector3.Distance(player.position, lastPlayerPos) > 0.01f;
        if (playerIsMoving)
        {
            shieldBreakTimer = 0f;
        }
        else if (agent.velocity.magnitude < 0.1f)
        {
            shieldBreakTimer += Time.deltaTime;
            if (shieldBreakTimer >= SHIELD_BREAK_DELAY)
            {
                BreakShield();
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
            SetAnimationState(false, true);
        }
        else
        {
            SetAnimationState(true, false);
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
        SetAnimationState(false, true);
    }
    #endregion

    #region Public Event Functions
    public void BreakPhase2Shield()
    {
        if (currentState == AIState.Phase2_WaypointShield) BreakShield();
    }

    public void BreakPhase3ShieldByWreckingBall()
    {
        if (currentState == AIState.Phase3_WreckingBallShield) BreakShield();
    }
    #endregion

    #region Standard AI Behavior
    void DetectPlayer()
    {
        if (wasProvoked) return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        if (distanceToPlayer > detectionRange) return;

        Vector3 directionToPlayer = (player.position - transform.position).normalized;
        if (Vector3.Angle(transform.forward, directionToPlayer) > fieldOfView / 2) return;

        Vector3 rayOrigin = transform.position + Vector3.up * 1.5f;
        if (!Physics.Raycast(rayOrigin, directionToPlayer, distanceToPlayer, obstacleLayerMask))
        {
            wasProvoked = true;
            StartCoroutine(TriggerRage());
        }
    }

    IEnumerator TriggerRage()
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
            SetAnimationState(false, true);
            return;
        }
        if (!agent.pathPending && agent.remainingDistance < agent.stoppingDistance)
        {
            currentWanderWaypointIndex = (currentWanderWaypointIndex + 1) % wanderWaypoints.Length;
            agent.SetDestination(wanderWaypoints[currentWanderWaypointIndex].position);
        }
        SetAnimationState(true, false);
    }

    void Chase()
    {
        agent.speed = chaseSpeed;
        agent.isStopped = false;
        agent.SetDestination(player.position);
        SetAnimationState(true, false);

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
        SetAnimationState(false, false);

        if (Time.time - lastAttackTime >= attackCooldown)
        {
            lastAttackTime = Time.time;
            animator.SetTrigger("attack");
            PlayAttackSFX();
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
            StartCoroutine(TriggerRage());
        }
        PlaySFX(hurtSFX);
    }

    public void Die()
    {
        // Cek apakah sedang dalam fase perisai yang butuh dihancurkan secara eksternal
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
            // Ganti PlayerStats dengan skrip health pemain Anda
            // player.GetComponent<PlayerStats>()?.Damage(meleeDamage, false);
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

    #region Utility
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
    #endregion
}