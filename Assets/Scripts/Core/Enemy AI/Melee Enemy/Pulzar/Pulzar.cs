using UnityEngine;
using UnityEngine.AI;
using cowsins;
using System.Collections;

public class Pulzar : MonoBehaviour
{
    // ======== STATE ========
    private enum AIState { Wandering, Chasing, Attacking, Raging, VulnerableShield, IndestructibleShield, Dead }
    private AIState currentState;

    // ======== WAYPOINT ========
    [Header("Waypoint Settings")]
    public Transform[] waypoints;
    private int currentWaypointIndex = 0;
    [Tooltip("Waypoint tujuan saat HP mencapai 100 (fase perisai terakhir).")]
    public Transform finalPhaseWaypoint;

    // ======== NAV ========
    [Header("Navigation & Movement Feel")]
    private NavMeshAgent agent;
    public float walkSpeed = 3f;
    public float chaseSpeed = 5f;
    public float angularSpeed = 240f;
    public float acceleration = 12f;

    // ======== PLAYER DETECTION (RAYCAST) ========
    [Header("Player Detection (Raycast)")]
    public Transform player;
    [Tooltip("Layer yang menghalangi LOS (tembok/dll).")]
    public LayerMask obstacleLayerMask;
    [Tooltip("Jarak deteksi maksimum.")]
    public float detectionRange = 15f;
    [Tooltip("Field-of-view derajat.")]
    [Range(0f, 360f)] public float fieldOfView = 120f;
    [Tooltip("Tinggi 'mata' untuk raycast.")]
    public float eyeHeight = 1.5f;

    // ======== ATTACK ========
    [Header("Attack Settings")]
    public float attackRange = 2f;
    public float meleeDamage = 20f;
    public float attackCooldown = 2f;
    private float lastAttackTime;

    // ======== COMBAT / SFX ========
    [Header("Combat Awareness")]
    private bool wasProvoked = false;

    [Header("SFX")]
    public AudioClip[] attackSFX;
    [SerializeField] AudioClip rageSFX;
    public AudioClip hurtSFX;
    public AudioClip deathSFX;
    public float sfxVolume = 1f;

    // ======== SHIELD ========
    [Header("Shield Settings")]
    [Tooltip("VFX perisai (nyala saat shield aktif).")]
    public GameObject shieldVFX;
    [Tooltip("Nilai shield saat aktif (besar agar praktis kebal).")]
    public float shieldAmount = 99999f;

    // Trigger per-phase HP
    private bool shieldTriggeredAt250, shieldTriggeredAt200, shieldTriggeredAt150, indestructibleShieldTriggered;
    private float shieldBreakTimer = 0f; // dipakai untuk fase-1 (stare timer)

    [Header("Fase-1: Pecah Shield dengan Saling Menatap & Diam")]
    [Tooltip("Durasi saling menatap & diam agar shield fase-1 pecah.")]
    public float stareHoldTime = 3f;
    [Tooltip("Ambang sudut hadap (deg) untuk dianggap saling menatap.")]
    public float faceAngleDeg = 20f;
    [Tooltip("Wajib LOS jelas?")]
    public bool requireLineOfSight = true;
    [Tooltip("Maks. kecepatan NavMesh agar musuh dianggap diam.")]
    public float enemyStillSpeed = 0.05f;
    [Tooltip("Ambang pergeseran posisi per frame agar player dianggap diam.")]
    public float playerStillMove = 0.02f;

    // ======== ANTI GANGGUAN (RAGE x SARANG/TRAP) ========
    [Header("Anti Gangguan saat Rage")]
    [Tooltip("Layer perangkap/sarang yang ingin diabaikan kontaknya saat RAGE.")]
    public LayerMask trapLayers;
    [Tooltip("Tag perangkap yang ingin di-IgnoreCollision ketika menyentuh saat RAGE/SHEILD.")]
    public string[] trapTags;
    [Tooltip("Durasi kita mengabaikan collider perangkap ketika bersentuhan (detik).")]
    public float ignoreTrapSeconds = 1.0f;

    // ======== DEBUG ========
    [Header("Stats (debug)")]
    [SerializeField] float currentHealth;
    [SerializeField] float currentShield;

    [Header("Script Reference")]
    [SerializeField] NormalDoor normalDoor;

    // ======== RAGE LOCK ========
    [Header("Rage Lock")]
    [SerializeField] private float rageMinDuration = 1.2f;
    private bool rageAnimating = false;
    private bool rageLocked = false;

    // ======== INTERNALS ========
    private EnemyHealth enemyHealth;
    private Animator animator;
    private Vector3 lastPlayerPos;
    private Collider enemyCollider;
    private AudioSource audioSource;
    private Rigidbody rb;

    [SerializeField] private bool shieldLock = false;  // true = kebal mutlak (kunci manual)
    private float healthAtIndestructible = -1f;

    private bool IsInShieldState => currentState == AIState.VulnerableShield || currentState == AIState.IndestructibleShield;
    private bool IsBusy => currentState == AIState.Dead || rageAnimating || rageLocked;

    // ========= LIFECYCLE =========
    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        enemyHealth = GetComponent<EnemyHealth>();
        enemyCollider = GetComponent<Collider>();
        rb = GetComponent<Rigidbody>();

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.spatialBlend = 1f;
        audioSource.playOnAwake = false;
        audioSource.volume = sfxVolume;

        agent.updateRotation = true;
        agent.updateUpAxis = true;
    }

    void Start()
    {
        currentState = AIState.Wandering;

        // awal: shield mati
        SetShieldActive(false, false);

        agent.speed = walkSpeed;
        agent.angularSpeed = angularSpeed;
        agent.acceleration = acceleration;

        if (waypoints != null && waypoints.Length > 0)
            agent.SetDestination(waypoints[0].position);
    }

    void Update()
    {
        currentHealth = enemyHealth.health;
        currentShield = enemyHealth.shield;
        if (player == null || currentState == AIState.Dead) return;

        // Pastikan shield tetap penuh saat terkunci
        if (shieldLock)
        {
            if (enemyHealth.shield < shieldAmount) enemyHealth.shield = shieldAmount;
            if (shieldVFX != null && !shieldVFX.activeSelf) shieldVFX.SetActive(true);
        }

        // “God mode” HP saat indestructible
        if (currentState == AIState.IndestructibleShield)
        {
            if (healthAtIndestructible < 0f) healthAtIndestructible = enemyHealth.health;
            if (enemyHealth.health < healthAtIndestructible) enemyHealth.health = healthAtIndestructible;
        }

        // Cek trigger fase (kecuali sedang di vulnerable agar tidak saling tindih)
        if (!rageAnimating && !rageLocked && currentState != AIState.VulnerableShield)
            CheckShieldTriggers();

        // AI logic
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
                DetectPlayer_Raycast();
                break;

            case AIState.Chasing:
                if (!IsBusy && !IsInShieldState)
                {
                    DetectPlayer_Raycast(); // tetap update awareness
                    Chase();
                }
                break;

            case AIState.Attacking:
                if (!IsBusy && !IsInShieldState) Attack();
                break;
        }

        lastPlayerPos = player.position;
    }

    // ========= SHIELD CORE =========
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
            shieldLock = false;
            enemyHealth.shield = 0f;
            if (shieldVFX != null) shieldVFX.SetActive(false);
        }
    }

    void CheckShieldTriggers()
    {
        float hp = enemyHealth.health;

        // Fase-1 (kini kebal terkunci, pecah hanya via tatap-diam)
        if (hp <= 250 && !shieldTriggeredAt250) ActivateVulnerableShield(ref shieldTriggeredAt250);
        else if (hp <= 200 && !shieldTriggeredAt200) ActivateVulnerableShield(ref shieldTriggeredAt200);
        else if (hp <= 150 && !shieldTriggeredAt150) ActivateVulnerableShield(ref shieldTriggeredAt150);
        // Fase terakhir
        else if (hp <= 100 && !indestructibleShieldTriggered) ActivateIndestructibleShield();
    }

    void ActivateVulnerableShield(ref bool trigFlag)
    {
        trigFlag = true;
        currentState = AIState.VulnerableShield;

        // Kunci shield (benar-benar kebal)
        SetShieldActive(true, true);

        agent.isStopped = false; // tetap bisa bergerak mengejar saat player bergerak
        agent.speed = chaseSpeed;
        shieldBreakTimer = 0f;
        SetAnimationState(true, false);
    }

    void ActivateIndestructibleShield()
    {
        indestructibleShieldTriggered = true;
        currentState = AIState.IndestructibleShield;

        SetShieldActive(true, true);
        healthAtIndestructible = enemyHealth.health;

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
            agent.speed = walkSpeed;
            agent.SetDestination(finalPhaseWaypoint.position);
            SetAnimationState(true, false);
        }
    }

    void HandleVulnerableShield()
    {
        if (player == null) return;

        float dist = Vector3.Distance(transform.position, player.position);
        bool enemyStill = agent.velocity.magnitude <= enemyStillSpeed || agent.isStopped;
        bool playerStill = Vector3.Distance(player.position, lastPlayerPos) <= playerStillMove;
        bool facingEachOther = IsFacingEachOther(faceAngleDeg);
        bool losOK = !requireLineOfSight || HasClearLOS();

        // Jika player bergerak → kejar sambil shield tetap kebal
        if (!playerStill)
        {
            agent.isStopped = false;
            agent.speed = chaseSpeed;
            agent.SetDestination(player.position);
            SetAnimationState(true, false);
            shieldBreakTimer = 0f;
            return;
        }

        // Player diam: bila cukup dekat → berhenti & menatap
        if (dist <= attackRange * 1.25f)
        {
            agent.isStopped = true;
            SetAnimationState(false, true);

            // putar menghadap player
            Vector3 dir = player.position - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.001f)
            {
                Quaternion look = Quaternion.LookRotation(dir.normalized, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, look, Time.deltaTime * 5f);
            }

            if (enemyStill && facingEachOther && losOK)
            {
                shieldBreakTimer += Time.deltaTime;
                if (shieldBreakTimer >= stareHoldTime)
                {
                    BreakVulnerableShield();
                    return;
                }
            }
            else shieldBreakTimer = 0f;
        }
        else
        {
            // Jauh tapi player diam → dekati pelan
            agent.isStopped = false;
            agent.speed = walkSpeed;
            agent.SetDestination(player.position);
            SetAnimationState(true, false);
            shieldBreakTimer = 0f;
        }
    }

    void BreakVulnerableShield()
    {
        SetShieldActive(false, false);         // lepas kunci & matikan shield
        currentState = AIState.Chasing;        // lanjut lawan
        agent.isStopped = false;
        agent.speed = chaseSpeed;
        if (player != null) agent.SetDestination(player.position);
        shieldBreakTimer = 0f;
    }

    // ========= DETECT (RAYCAST) =========
    void DetectPlayer_Raycast()
    {
        if (player == null || wasProvoked) return;

        Vector3 origin = transform.position + Vector3.up * eyeHeight;
        Vector3 target = player.position + Vector3.up * eyeHeight;
        Vector3 toPlayer = target - origin;
        float dist = toPlayer.magnitude;
        if (dist > detectionRange) return;

        Vector3 dir = toPlayer / (dist > 0.0001f ? dist : 1f);

        // Cek FOV
        float angle = Vector3.Angle(transform.forward, dir);
        if (angle > fieldOfView * 0.5f) return;

        // Cek LOS (tidak menabrak obstacle)
        if (Physics.Raycast(origin, dir, dist, obstacleLayerMask)) return;

        // sukses deteksi
        if (!IsInShieldState && !rageAnimating && !rageLocked)
        {
            wasProvoked = true;
            StartCoroutine(TriggerRage());
        }
    }

    // ========= RAGE =========
    IEnumerator TriggerRage()
    {
        if (rageAnimating || rageLocked || currentState == AIState.Raging) yield break;

        currentState = AIState.Raging;
        rageAnimating = true;
        rageLocked = true;

        // Lindungi anim dari gangguan fisika
        SetShieldActive(true, true); // aman dari damage
        if (rb != null) rb.isKinematic = true; // cegah impulse dari "sarang"

        agent.isStopped = true;
        agent.velocity = Vector3.zero;
        agent.ResetPath();

        ResetAllAnimationStates();
        animator.SetBool("isRage", true);
        PlayRageSFX();

        yield return new WaitForSeconds(rageMinDuration);

        animator.SetBool("isRage", false);

        rageAnimating = false;
        rageLocked = false;

        // Selesai rage: bila tidak dalam shield state lain → lepas shield
        if (!IsInShieldState) SetShieldActive(false, false);
        if (rb != null) rb.isKinematic = false;

        currentState = AIState.Chasing;
        agent.isStopped = false;
    }

    // ========= BEHAVIOUR =========
    void Wander()
    {
        if (IsInShieldState || IsBusy) return;

        agent.speed = walkSpeed;

        if (waypoints == null || waypoints.Length == 0)
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
        if (IsInShieldState || IsBusy) return;

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
        if (IsInShieldState || IsBusy)
        {
            currentState = AIState.Chasing;
            return;
        }

        agent.isStopped = true;

        // hadap player
        Vector3 direction = (player.position - transform.position); direction.y = 0f;
        if (direction.sqrMagnitude > 0.001f)
        {
            Quaternion lookRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 5f);
        }

        SetAnimationState(false, false);

        // serang by cooldown
        if (Time.time - lastAttackTime >= attackCooldown)
        {
            lastAttackTime = Time.time;
            animator.SetTrigger("attack");
            PlayAttackSFX();
        }

        // keluar attack jika player kabur
        if (Vector3.Distance(transform.position, player.position) > attackRange)
        {
            currentState = AIState.Chasing;
        }
    }

    // ========= DAMAGE / EVENTS =========
    public void OnTakeDamage()
    {
        // Saat shield terkunci atau aktif → abaikan (kebal)
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
        if (IsInShieldState || shieldLock || enemyHealth.shield > 0) SetShieldActive(false, false);

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

        if (rb != null) rb.isKinematic = true;

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
        if (currentState != AIState.Attacking || enemyHealth.shield > 0) return;

        if (Vector3.Distance(transform.position, player.position) <= attackRange)
        {
            var stats = player.GetComponent<PlayerStats>();
            if (stats != null) stats.Damage(meleeDamage, false);
        }
    }

    public void PlayRageSFX()
    {
        if (rageSFX != null && audioSource != null) audioSource.PlayOneShot(rageSFX, sfxVolume);
    }
    public void PlayAttackSFX()
    {
        if (attackSFX == null || attackSFX.Length == 0 || audioSource == null) return;
        var clip = attackSFX[Random.Range(0, attackSFX.Length)];
        audioSource.PlayOneShot(clip, sfxVolume);
    }

    // ========= HELPER: Facing & LOS =========
    bool IsFacingEachOther(float maxAngleDeg)
    {
        if (player == null) return false;

        Vector3 toPlayer = (player.position - transform.position); toPlayer.y = 0f;
        Vector3 toEnemy = (transform.position - player.position); toEnemy.y = 0f;

        if (toPlayer.sqrMagnitude < 0.0001f || toEnemy.sqrMagnitude < 0.0001f) return false;

        float a1 = Vector3.Angle(transform.forward, toPlayer.normalized);
        float a2 = Vector3.Angle(player.forward, toEnemy.normalized);

        return (a1 <= maxAngleDeg) && (a2 <= maxAngleDeg);
    }

    bool HasClearLOS()
    {
        if (player == null) return false;
        Vector3 origin = transform.position + Vector3.up * eyeHeight;
        Vector3 target = player.position + Vector3.up * eyeHeight;
        Vector3 dir = (target - origin);
        float dist = dir.magnitude;
        if (dist <= 0.001f) return false;
        dir /= dist;

        // true jika TIDAK menabrak obstacle
        return !Physics.Raycast(origin, dir, dist, obstacleLayerMask);
    }

    private void SetAnimationState(bool isWalking, bool isIdle)
    {
        animator.SetBool("isWalking", isWalking);
        animator.SetBool("isIdle", isIdle);
    }

    private void ResetAllAnimationStates()
    {
        animator.SetBool("isWalking", false);
        animator.SetBool("isIdle", false);
        animator.SetBool("isRage", false);
        animator.ResetTrigger("attack");
    }

    // ========= ANTI GANGGUAN "SARANG" =========
    private void OnCollisionEnter(Collision collision)
    {
        // Jika sedang rage/ber-shield, abaikan perangkap yang mengganggu anim
        if ((rageAnimating || shieldLock || enemyHealth.shield > 0) && ShouldTreatAsTrap(collision.gameObject))
        {
            if (enemyCollider != null && collision.collider != null)
                StartCoroutine(TemporarilyIgnoreCollider(collision.collider, enemyCollider, ignoreTrapSeconds));
        }

        if (collision.gameObject.CompareTag("FallenObject"))
        {
            Die();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if ((rageAnimating || shieldLock || enemyHealth.shield > 0) && ShouldTreatAsTrap(other.gameObject))
        {
            if (enemyCollider != null)
                StartCoroutine(TemporarilyIgnoreCollider(other, enemyCollider, ignoreTrapSeconds));
        }
    }

    bool ShouldTreatAsTrap(GameObject go)
    {
        bool layerMatch = (trapLayers.value == 0) || ((trapLayers.value & (1 << go.layer)) != 0);
        bool tagMatch = (trapTags == null || trapTags.Length == 0);
        if (!tagMatch)
        {
            for (int i = 0; i < trapTags.Length; i++)
                if (!string.IsNullOrEmpty(trapTags[i]) && go.CompareTag(trapTags[i])) { tagMatch = true; break; }
        }
        return layerMatch || tagMatch;
    }

    IEnumerator TemporarilyIgnoreCollider(Collider trapCol, Collider selfCol, float seconds)
    {
        if (trapCol == null || selfCol == null) yield break;
        Physics.IgnoreCollision(trapCol, selfCol, true);
        yield return new WaitForSeconds(seconds);
        if (trapCol != null && selfCol != null)
            Physics.IgnoreCollision(trapCol, selfCol, false);
    }

    // ========= GIZMOS =========
    void OnDrawGizmosSelected()
    {
        // Deteksi
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        // Attack
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        // LOS debug
        if (player != null)
        {
            Gizmos.color = Color.cyan;
            Vector3 origin = transform.position + Vector3.up * eyeHeight;
            Vector3 head = player.position + Vector3.up * eyeHeight;
            Gizmos.DrawLine(origin, head);
        }
    }
}
