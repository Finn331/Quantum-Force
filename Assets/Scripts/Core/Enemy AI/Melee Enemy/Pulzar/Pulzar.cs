using UnityEngine;
using UnityEngine.AI;
using cowsins;
using System.Collections;

public class Pulzar : MonoBehaviour
{
    // State Machine
    private enum AIState { Wandering, Chasing, Attacking, Raging, VulnerableShield, IndestructibleShield, Dead }
    private AIState currentState;

    [Header("Waypoint Settings")]
    public Transform[] waypoints;
    private int currentWaypointIndex = 0;

    [Tooltip("Waypoint tujuan saat HP mencapai 100 (fase perisai terakhir).")]
    public Transform finalPhaseWaypoint;

    [Header("Navigation & Movement Feel")]
    private NavMeshAgent agent;
    public float walkSpeed = 3f;
    public float chaseSpeed = 5f;
    [Tooltip("Semakin rendah, belok makin halus.")]
    public float angularSpeed = 240f;
    [Tooltip("Semakin rendah, akselerasi makin halus.")]
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
    [Tooltip("VFX perisai (nyala saat shield aktif).")]
    public GameObject shieldVFX;
    [Tooltip("Nilai shield saat aktif (besar agar praktis kebal).")]
    public float shieldAmount = 99999f;

    // Trigger per-phase HP
    private bool shieldTriggeredAt250, shieldTriggeredAt200, shieldTriggeredAt150, indestructibleShieldTriggered;
    private float shieldBreakTimer = 0f;

    [Header("Stats (debug)")]
    [SerializeField] float currentHealth;
    [SerializeField] float currentShield;

    [Header("Script Reference")]
    [SerializeField] NormalDoor normalDoor;

    // --- RAGE LOCK ---
    [Header("Rage Lock")]
    [SerializeField] private float rageMinDuration = 1.2f; // durasi minimal anim rage
    private bool rageAnimating = false;   // sedang memainkan anim rage
    private bool rageLocked = false;      // kunci state saat rage berjalan

    // --- SHIELD LOCK UTILS ---
    [Tooltip("Mengunci shield agar tidak bisa turun saat fase tertentu (mis. rage).")]
    [SerializeField] private bool shieldLock = false;

    // refs
    private EnemyHealth enemyHealth;
    private Animator animator;
    private Vector3 lastPlayerPos;
    private Vector3 lastEnemyPos;
    private Collider enemyCollider;
    private AudioSource audioSource;

    private bool IsInShieldState => currentState == AIState.VulnerableShield || currentState == AIState.IndestructibleShield;
    private bool IsBusy => currentState == AIState.Dead || rageAnimating || rageLocked;

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

        // agent setup
        agent.updateRotation = true;
        agent.updateUpAxis = true;
    }

    void Start()
    {
        currentState = AIState.Wandering;

        // pastikan awalnya shield mati
        SetShieldActive(false, false);

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

        // Jaga shield ketika lock ON (mis. saat rage): pastikan nilainya tidak drop
        if (shieldLock && enemyHealth.shield < shieldAmount)
        {
            enemyHealth.shield = shieldAmount;
            if (shieldVFX != null && !shieldVFX.activeSelf) shieldVFX.SetActive(true);
        }

        // Shield triggers hanya saat TIDAK rage & bukan sedang vulnerable shield
        if (!rageAnimating && !rageLocked && currentState != AIState.VulnerableShield)
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
                // dikunci oleh coroutine TriggerRage()
                break;

            case AIState.Wandering:
                Wander();
                DetectPlayer(); // boleh provoke dari wander
                break;

            case AIState.Chasing:
                if (!rageAnimating && !rageLocked && !IsInShieldState) Chase();
                break;

            case AIState.Attacking:
                if (!rageAnimating && !rageLocked && !IsInShieldState) Attack();
                break;
        }

        lastPlayerPos = player.position;
        lastEnemyPos = transform.position;
    }

    // ===================== SHIELD =====================

    void SetShieldActive(bool active, bool lockShield)
    {
        shieldLock = lockShield;

        if (active)
        {
            enemyHealth.shield = shieldAmount;
            if (shieldVFX != null) shieldVFX.SetActive(true);
        }
        else
        {
            shieldLock = false; // selalu lepas lock saat mematikan shield
            enemyHealth.shield = 0;
            if (shieldVFX != null) shieldVFX.SetActive(false);
        }
    }

    void CheckShieldTriggers()
    {
        float hp = enemyHealth.health;
        if (hp <= 250 && !shieldTriggeredAt250) ActivateVulnerableShield(ref shieldTriggeredAt250);
        else if (hp <= 200 && !shieldTriggeredAt200) ActivateVulnerableShield(ref shieldTriggeredAt200);
        else if (hp <= 150 && !shieldTriggeredAt150) ActivateVulnerableShield(ref shieldTriggeredAt150);
        else if (hp <= 100 && !indestructibleShieldTriggered) ActivateIndestructibleShield();
    }

    void ActivateVulnerableShield(ref bool triggerFlag)
    {
        triggerFlag = true;
        currentState = AIState.VulnerableShield;

        // aktifkan shield TANPA lock (boleh pecah oleh kondisi "diam 5 detik")
        SetShieldActive(true, false);

        agent.isStopped = true;
        agent.velocity = Vector3.zero;

        SetAnimationState(isWalking: false, isIdle: true);
        shieldBreakTimer = 0f;
    }

    void ActivateIndestructibleShield()
    {
        indestructibleShieldTriggered = true;
        currentState = AIState.IndestructibleShield;

        // aktifkan shield TANPA lock (tetap aktif selama fase ini)
        SetShieldActive(true, false);

        if (finalPhaseWaypoint != null)
        {
            agent.isStopped = false;
            agent.speed = walkSpeed;
            agent.SetDestination(finalPhaseWaypoint.position);
            SetAnimationState(isWalking: true, isIdle: false);
        }
        else
        {
            agent.isStopped = true;
            SetAnimationState(isWalking: false, isIdle: true);
        }
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
            agent.isStopped = false;
            SetAnimationState(true, false);
        }
    }

    void HandleVulnerableShield()
    {
        // Saat shield vuln, player yang diam 5 detik => shield pecah
        bool playerIsMoving = Vector3.Distance(player.position, lastPlayerPos) > 0.01f;

        if (playerIsMoving)
        {
            float dist = Vector3.Distance(transform.position, player.position);
            if (dist > attackRange)
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
                    // pecahkan shield
                    SetShieldActive(false, false);
                    shieldBreakTimer = 0f;

                    currentState = AIState.Chasing;
                    agent.isStopped = false;
                }
            }
            else shieldBreakTimer = 0f;
        }
    }

    // ===================== PERSEPSI =====================

    void DetectPlayer()
    {
        if (player == null || wasProvoked) return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        if (distanceToPlayer > detectionRange) return;

        Vector3 dir = (player.position - transform.position).normalized;
        if (Vector3.Angle(transform.forward, dir) > fieldOfView / 2) return;

        Vector3 rayOrigin = transform.position + Vector3.up * 0.5f;

        // Jika TIDAK menabrak obstacle = LOS jelas
        if (!Physics.Raycast(rayOrigin, dir, distanceToPlayer, obstacleLayerMask))
        {
            // Jangan provoke bila dalam shield/rage
            if (IsInShieldState || rageAnimating || rageLocked) return;

            wasProvoked = true; // set dulu agar tidak dobel
            StartCoroutine(TriggerRage());
        }
    }

    // ===================== RAGE =====================

    IEnumerator TriggerRage()
    {
        if (rageAnimating || rageLocked || currentState == AIState.Raging) yield break;

        currentState = AIState.Raging;
        rageAnimating = true;
        rageLocked = true;

        // Saat saling menatap (rage), AKTIFKAN SHIELD & KUNCI agar tidak bisa diserang
        SetShieldActive(true, true);

        agent.isStopped = true;
        agent.velocity = Vector3.zero;
        agent.ResetPath();

        ResetAllAnimationStates();
        animator.SetBool("isRage", true);
        PlayRageSFX();

        // Minimal durasi rage agar tidak terpotong damage/event lain
        yield return new WaitForSeconds(rageMinDuration);

        animator.SetBool("isRage", false);

        // Lepaskan kunci & matikan shield jika tidak masuk fase shield lain
        rageAnimating = false;
        rageLocked = false;

        if (!IsInShieldState) SetShieldActive(false, false);

        currentState = AIState.Chasing;
        agent.isStopped = false;
    }

    // ===================== BEHAVIOUR =====================

    void Wander()
    {
        if (IsInShieldState || rageAnimating || rageLocked) return;

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

        agent.isStopped = false;
        SetAnimationState(true, false);
    }

    void Chase()
    {
        if (IsInShieldState || rageAnimating || rageLocked) return;

        agent.speed = chaseSpeed;

        float distance = Vector3.Distance(transform.position, player.position);
        if (distance <= attackRange)
        {
            currentState = AIState.Attacking;
            return;
        }

        agent.isStopped = false;
        agent.SetDestination(player.position);
        SetAnimationState(true, false);
    }

    void Attack()
    {
        if (IsInShieldState || rageAnimating || rageLocked)
        {
            currentState = AIState.Chasing; // safety fallback
            return;
        }

        agent.isStopped = true;

        // Hadap ke player
        Vector3 direction = (player.position - transform.position).normalized;
        Quaternion lookRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 5f);

        SetAnimationState(false, false);

        // Trigger serang by cooldown
        if (Time.time - lastAttackTime >= attackCooldown)
        {
            lastAttackTime = Time.time;
            animator.SetTrigger("attack");
            PlayAttackSFX();
        }

        // Keluar dari mode attack jika player kabur
        if (Vector3.Distance(transform.position, player.position) > attackRange)
        {
            currentState = AIState.Chasing;
        }
    }

    // ===================== EVENT / DAMAGE =====================

    public void OnTakeDamage()
    {
        // Saat shield terkunci (rage) atau shield aktif, jangan ubah state (cukup SFX)
        if (shieldLock || enemyHealth.shield > 0 || rageAnimating || rageLocked)
        {
            if (hurtSFX != null && audioSource != null) audioSource.PlayOneShot(hurtSFX);
            return;
        }

        // Bila masih wander & belum provoke → rage sekali
        if (currentState == AIState.Wandering && !wasProvoked && !IsInShieldState)
        {
            wasProvoked = true;
            StartCoroutine(TriggerRage());
        }

        if (hurtSFX != null && audioSource != null) audioSource.PlayOneShot(hurtSFX);
    }

    public void Die()
    {
        // Jika mati di fase shield terakhir, matikan shield & VFX
        if (currentState == AIState.IndestructibleShield || shieldLock || enemyHealth.shield > 0)
        {
            SetShieldActive(false, false);
        }

        if (normalDoor != null) normalDoor.enabled = true;
        DieTrigger();
    }

    private void DieTrigger()
    {
        currentState = AIState.Dead;

        if (agent != null)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
            agent.ResetPath();
        }

        ResetAllAnimationStates();
        animator.SetBool("isDie", true);
        animator.SetTrigger("die");

        if (deathSFX != null && audioSource != null) audioSource.PlayOneShot(deathSFX);
        if (enemyCollider != null) enemyCollider.enabled = false;

        Destroy(gameObject, 3f);
    }

    // Dipanggil oleh Animation Event pada frame hit
    public void DealMeleeDamage()
    {
        // Jangan damage bila bukan attacking atau shield aktif (invulnerable phase)
        if (currentState != AIState.Attacking || enemyHealth.shield > 0) return;

        if (Vector3.Distance(transform.position, player.position) <= attackRange)
        {
            var stats = player.GetComponent<PlayerStats>();
            if (stats != null) stats.Damage(meleeDamage, false);
        }
    }

    public void PlayRageSFX() { if (rageSFX != null && audioSource != null) audioSource.PlayOneShot(rageSFX, sfxVolume); }
    public void PlayAttackSFX()
    {
        if (attackSFX == null || attackSFX.Length == 0 || audioSource == null) return;
        var clip = attackSFX[Random.Range(0, attackSFX.Length)];
        audioSource.PlayOneShot(clip, sfxVolume);
    }

    // ===================== ANIM / UTILS =====================

    private void SetAnimationState(bool isWalking, bool isIdle)
    {
        animator.SetBool("isWalking", isWalking);
        animator.SetBool("isIdle", isIdle);
        // jika punya isRunning di controller-mu, set di sini
        // animator.SetBool("isRunning", isWalking && agent.speed > walkSpeed);
    }

    private void ResetAllAnimationStates()
    {
        animator.SetBool("isWalking", false);
        animator.SetBool("isIdle", false);
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
            Vector3 fov1 = Quaternion.AngleAxis(fieldOfView / 2, transform.up) * transform.forward * detectionRange;
            Vector3 fov2 = Quaternion.AngleAxis(-fieldOfView / 2, transform.up) * transform.forward * detectionRange;
            Gizmos.color = Color.cyan;
            Gizmos.DrawRay(transform.position, fov1);
            Gizmos.DrawRay(transform.position, fov2);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("FallenObject"))
        {
            Die();
        }
    }
}
