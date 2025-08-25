using UnityEngine;
using UnityEngine.AI;
using cowsins;
using System.Collections;
using System.Linq;

public class Singulra : MonoBehaviour
{
    private enum AIState
    {
        Wandering,
        Chasing,
        Attacking,
        Raging,
        Stunned,
        Phase1_VulnerableShield,
        Phase2_WaypointShield,
        Dead
    }
    private AIState currentState;

    private enum Phase2Sub { None, A, B }
    private Phase2Sub currentPhase2 = Phase2Sub.None;

    private NavMeshAgent agent;
    private Animator animator;
    private EnemyHealth enemyHealth;
    private AudioSource audioSource;
    private Collider enemyCollider;

    [Header("Target")]
    public Transform player;

    [Header("Phase Settings (Health Thresholds)")]
    public float maxHealth = 1500f;
    private const float phase1_Threshold = 1250f;
    private const float phase2a_Threshold = 1000f;
    private const float phase2b_Threshold = 750f;

    private bool phase1Triggered, phase2aTriggered, phase2bTriggered;

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
    [Range(0, 360)] public float fieldOfViewAngle = 120f;
    public float eyeHeight = 1.5f;

    [Header("Stun Settings")]
    public float stunDuration = 3f;

    [Header("Attack Settings")]
    public float meleeDamage = 20f;
    public float attackCooldown = 2f;
    private float lastAttackTime;
    private bool wasProvoked = false;

    [Header("Attack Movement Tuning")]
    [Tooltip("Distance to ENTER attack (slightly smaller than stoppingDistance so it stops quickly).")]
    public float attackEnterDistance = 2.6f;
    [Tooltip("Distance to EXIT attack (slightly larger than stoppingDistance to avoid flip-flop).")]
    public float attackExitDistance = 3.4f;
    [Tooltip("Tiny halt when entering Attack to ensure feet plant before anim trigger.")]
    public float attackEnterHaltTime = 0.05f;

    [Header("Instant Attack On Sight")]
    [Tooltip("If player is visible and distance <= this value, do instant attack.")]
    public float instantAttackDistance = 2.8f;
    public enum AttackChoice { Attack, Attack2, Random }

    [Tooltip("Default instant attack outside special phases.")]
    public AttackChoice defaultAttack = AttackChoice.Attack;
    [Tooltip("Instant attack during Phase1 (Vulnerable Shield).")]
    public AttackChoice phase1Attack = AttackChoice.Attack;
    [Tooltip("Instant attack during Phase2 (Waypoint Shield).")]
    public AttackChoice phase2Attack = AttackChoice.Attack2;
    [Tooltip("Instant attack when enraged (low HP).")]
    public AttackChoice enragedAttack = AttackChoice.Random;

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

    [Header("Phase2 Modular Break (Collision)")]
    public string[] phase2a_BreakTags;
    public string[] phase2b_BreakTags;
    public bool destroyProjectileOnHit = true;
    public LayerMask phase2ProjectileLayerFilter = 0;

    private Vector3 lastPlayerPos;

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

        agent.updateRotation = true;
    }

    private void Start()
    {
        currentState = AIState.Wandering;

        enemyHealth.health = maxHealth;
        enemyHealth.shield = 0;
        if (shieldVFX != null) shieldVFX.SetActive(false);

        agent.speed = walkSpeed;
        agent.angularSpeed = angularSpeed;
        agent.acceleration = acceleration;
        agent.stoppingDistance = attackRange;

        if (wanderWaypoints != null && wanderWaypoints.Length > 0)
            agent.SetDestination(wanderWaypoints[currentWanderWaypointIndex].position);
    }

    private void Update()
    {
        if (player == null || currentState == AIState.Dead) return;

        if (currentState == AIState.Wandering || currentState == AIState.Chasing || currentState == AIState.Attacking)
            CheckPhaseTriggers();

        if (currentState == AIState.Wandering || currentState == AIState.Chasing)
        {
            float speedMag = agent.desiredVelocity.magnitude;
            animator.SetBool("isRunning", speedMag > 0.1f && !agent.isStopped);
            animator.SetBool("isWalking", speedMag > 0.1f && !agent.isStopped);
            animator.SetBool("isIdle", speedMag <= 0.1f || agent.isStopped);
        }

        switch (currentState)
        {
            case AIState.Stunned: break;
            case AIState.Phase1_VulnerableShield: HandlePhase1Shield(); break;
            case AIState.Phase2_WaypointShield: HandlePhase2Shield(); break;
            case AIState.Raging: break;
            case AIState.Wandering: Wander(); DetectPlayer(); break;
            case AIState.Chasing: Chase(); DetectPlayer(); break;
            case AIState.Attacking: Attack(); break;
        }

        lastPlayerPos = player.position;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("FallenObject") && currentState != AIState.Dead && currentState != AIState.Stunned)
            StartCoroutine(GetStunned());
    }

    private IEnumerator GetStunned()
    {
        AIState prev = currentState;
        currentState = AIState.Stunned;

        StopAgentHard();

        ResetAllAnimationStates();
        animator.SetTrigger("stunned");

        yield return new WaitForSeconds(stunDuration);

        currentState = prev;
        if (currentState != AIState.Dead) agent.isStopped = false;
    }

    private void CheckPhaseTriggers()
    {
        float currentHP = enemyHealth.health;

        if (!isEnraged && currentHP <= maxHealth * ENRAGE_THRESHOLD)
            ActivateEnrageMode();

        if (currentHP <= phase2b_Threshold && !phase2bTriggered)
        {
            StartCoroutine(EnterPhase_Phase2(Phase2Sub.B, phase2b_Waypoint));
        }
        else if (currentHP <= phase2a_Threshold && !phase2aTriggered)
        {
            StartCoroutine(EnterPhase_Phase2(Phase2Sub.A, phase2a_Waypoint));
        }
        else if (currentHP <= phase1_Threshold && !phase1Triggered)
        {
            StartCoroutine(EnterPhase_Phase1());
        }
    }

    private void ActivateEnrageMode()
    {
        isEnraged = true;
        chaseSpeed = rageChaseSpeed;
        meleeDamage = rageMeleeDamage;
        Debug.Log("ENRAGE MODE ACTIVATED!");
    }

    private IEnumerator EnterPhase_Phase1()
    {
        currentState = AIState.Raging;
        StopAgentHard();

        ResetAllAnimationStates();
        animator.SetBool("isRage", true);
        PlaySFX(rageSFX);

        yield return new WaitForSeconds(2.0f);

        animator.SetBool("isRage", false);
        ActivatePhase1Shield();
    }

    private IEnumerator EnterPhase_Phase2(Phase2Sub which, Transform waypoint)
    {
        currentState = AIState.Raging;
        StopAgentHard();

        ResetAllAnimationStates();
        animator.SetBool("isRage", true);
        PlaySFX(rageSFX);

        yield return new WaitForSeconds(2.0f);

        animator.SetBool("isRage", false);
        ActivatePhase2Shield(which, waypoint);
    }

    private void ActivatePhase1Shield()
    {
        phase1Triggered = true;
        currentState = AIState.Phase1_VulnerableShield;
        currentPhase2 = Phase2Sub.None;

        ActivateShieldVisuals();

        agent.isStopped = true;
        SetAnimationState(false, false, true);
        shieldBreakTimer = 0f;
    }

    private void HandlePhase1Shield()
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
                    BreakShield();
            }
        }
    }

    private void ActivatePhase2Shield(Phase2Sub which, Transform waypoint)
    {
        if (which == Phase2Sub.A) phase2aTriggered = true;
        else if (which == Phase2Sub.B) phase2bTriggered = true;

        currentState = AIState.Phase2_WaypointShield;
        currentPhase2 = which;

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
            SetAnimationState(false, false, true);
        }
    }

    private void HandlePhase2Shield()
    {
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            agent.isStopped = true;
            SetAnimationState(false, false, true);
        }
        else
        {
            agent.isStopped = false;
            SetAnimationState(true, false, false);
        }
    }

    public void BreakPhase2Shield()
    {
        if (currentState == AIState.Phase2_WaypointShield) BreakShield();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (currentState != AIState.Phase2_WaypointShield || enemyHealth.shield <= 0) return;

        if (phase2ProjectileLayerFilter.value != 0)
        {
            int bit = 1 << collision.gameObject.layer;
            if ((phase2ProjectileLayerFilter.value & bit) == 0) return;
        }

        string tagHit = collision.gameObject.tag;

        bool canBreak = false;
        if (currentPhase2 == Phase2Sub.A && phase2a_BreakTags != null && phase2a_BreakTags.Length > 0)
            canBreak = phase2a_BreakTags.Contains(tagHit);
        else if (currentPhase2 == Phase2Sub.B && phase2b_BreakTags != null && phase2b_BreakTags.Length > 0)
            canBreak = phase2b_BreakTags.Contains(tagHit);

        if (canBreak)
        {
            BreakShield();
            if (destroyProjectileOnHit) Destroy(collision.gameObject);
        }
    }

    // Instant attack when target is in sight
    private void DetectPlayer()
    {
        if (player == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        if (distanceToPlayer > detectionRange) return;

        Vector3 dir = (player.position - transform.position).normalized;
        if (Vector3.Angle(transform.forward, dir) >= fieldOfViewAngle / 2) return;

        Vector3 eyePosition = transform.position + Vector3.up * eyeHeight;

        if (!Physics.Raycast(eyePosition, dir, distanceToPlayer, visionBlockLayer))
        {
            if (distanceToPlayer <= instantAttackDistance)
            {
                TriggerInstantAttackByPhase();
                wasProvoked = true;
                return;
            }

            if (!wasProvoked)
            {
                wasProvoked = true;
                StartCoroutine(TriggerInitialRage());
            }
        }
    }

    private void TriggerInstantAttackByPhase()
    {
        currentState = AIState.Attacking;
        StopAgentHard();
        FacePlayerInstant();

        AttackChoice choice = defaultAttack;
        if (isEnraged) choice = enragedAttack;
        else if (currentState == AIState.Phase1_VulnerableShield) choice = phase1Attack;
        else if (currentState == AIState.Attacking && (phase1Triggered || phase2aTriggered || phase2bTriggered))
        {
            if (phase2aTriggered || phase2bTriggered) choice = phase2Attack;
        }

        DoAttackTrigger(choice);
        lastAttackTime = Time.time;
    }

    private void DoAttackTrigger(AttackChoice choice)
    {
        switch (choice)
        {
            case AttackChoice.Attack: animator.SetTrigger("attack"); break;
            case AttackChoice.Attack2: animator.SetTrigger("attack2"); break;
            case AttackChoice.Random:
                if (Random.value < 0.5f) animator.SetTrigger("attack");
                else animator.SetTrigger("attack2");
                break;
        }

        animator.SetBool("isRunning", false);
        animator.SetBool("isWalking", false);
        animator.SetBool("isIdle", false);
    }

    private void FacePlayerInstant()
    {
        Vector3 toPlayer = player.position - transform.position;
        toPlayer.y = 0f;
        if (toPlayer.sqrMagnitude > 0.001f)
        {
            transform.rotation = Quaternion.LookRotation(toPlayer.normalized, Vector3.up);
        }
    }

    private IEnumerator TriggerInitialRage()
    {
        currentState = AIState.Raging;
        StopAgentHard();

        ResetAllAnimationStates();
        animator.SetBool("isRage", true);
        PlaySFX(rageSFX);

        yield return new WaitForSeconds(2.0f);

        animator.SetBool("isRage", false);
        currentState = AIState.Chasing;
        agent.isStopped = false;
    }

    private void Wander()
    {
        agent.speed = walkSpeed;

        if (wanderWaypoints == null || wanderWaypoints.Length == 0)
        {
            agent.isStopped = true;
            SetAnimationState(false, false, true);
            return;
        }

        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            currentWanderWaypointIndex = (currentWanderWaypointIndex + 1) % wanderWaypoints.Length;
            agent.SetDestination(wanderWaypoints[currentWanderWaypointIndex].position);
        }

        SetAnimationState(true, false, false);
    }

    private void Chase()
    {
        agent.speed = chaseSpeed;
        agent.isStopped = false;
        agent.SetDestination(player.position);

        float dist = Vector3.Distance(transform.position, player.position);
        if (dist <= attackEnterDistance)
        {
            SwitchToAttack();
            return;
        }

        SetAnimationState(false, true, false);
    }

    private void SwitchToAttack()
    {
        currentState = AIState.Attacking;
        StopAgentHard();
        animator.SetBool("isRunning", false);
        animator.SetBool("isWalking", false);
        animator.SetBool("isIdle", false);
        if (attackEnterHaltTime > 0f) StartCoroutine(HaltThenPrimeAttack());
    }

    private IEnumerator HaltThenPrimeAttack()
    {
        yield return new WaitForSeconds(attackEnterHaltTime);
    }

    private void Attack()
    {
        agent.updateRotation = false;

        Vector3 toPlayer = (player.position - transform.position);
        toPlayer.y = 0f;
        if (toPlayer.sqrMagnitude > 0.001f)
        {
            Quaternion face = Quaternion.LookRotation(toPlayer.normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, face, Time.deltaTime * 10f);
        }

        animator.SetBool("isRunning", false);
        animator.SetBool("isWalking", false);
        animator.SetBool("isIdle", false);

        if (Time.time - lastAttackTime >= attackCooldown)
        {
            lastAttackTime = Time.time;

            if (isEnraged)
            {
                int choice = Random.Range(0, 2);
                if (choice == 0) animator.SetTrigger("attack");
                else animator.SetTrigger("attack2");
            }
            else
            {
                animator.SetTrigger("attack");
            }
        }

        float dist = Vector3.Distance(transform.position, player.position);
        if (dist > attackExitDistance)
        {
            agent.updateRotation = true;
            currentState = AIState.Chasing;
        }
    }

    private void StopAgentHard()
    {
        agent.isStopped = true;
        agent.ResetPath();
        agent.velocity = Vector3.zero;
    }

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
        if (currentState == AIState.Phase2_WaypointShield) BreakShield();
        DieTrigger();
    }

    private void DieTrigger()
    {
        currentState = AIState.Dead;
        StopAgentHard();

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
        if (attackSFX == null || attackSFX.Length == 0) return;
        var clip = attackSFX[Random.Range(0, attackSFX.Length)];
        PlaySFX(clip);
    }

    private void PlaySFX(AudioClip clip)
    {
        if (clip != null)
        {
            SoundManager.Instance.PlaySound(clip, 0f, 0f, true, 0f);
        }
    }

    private void ActivateShieldVisuals()
    {
        enemyHealth.shield = shieldAmount;
        if (shieldVFX != null) shieldVFX.SetActive(true);
        PlaySFX(shieldUpSFX);
    }

    private void BreakShield()
    {
        enemyHealth.shield = 0;
        if (shieldVFX != null) shieldVFX.SetActive(false);
        PlaySFX(shieldDownSFX);

        currentState = AIState.Chasing;
        currentPhase2 = Phase2Sub.None;
        agent.isStopped = false;
        agent.updateRotation = true;
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
        animator.ResetTrigger("stunned");
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        if (player != null)
        {
            Vector3 fov1 = Quaternion.AngleAxis(fieldOfViewAngle / 2, transform.up) * transform.forward * detectionRange;
            Vector3 fov2 = Quaternion.AngleAxis(-fieldOfViewAngle / 2, transform.up) * transform.forward * detectionRange;
            Gizmos.color = Color.cyan;
            Gizmos.DrawRay(transform.position, fov1);
            Gizmos.DrawRay(transform.position, fov2);
        }
    }
}
