using UnityEngine;
using UnityEngine.AI;
using cowsins;
using System.Collections;

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
    private bool wasProvoked = false;
    private bool ragePlayed = false;
    private bool rageCoroutineRunning = false;
    private bool isRaging = false;

    [Header("SFX")]
    public AudioClip[] attackSFX;
    [SerializeField] AudioClip rageSFX;
    public AudioClip hurtSFX;
    public AudioClip deathSFX;
    public float sfxVolume = 1f;

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
    private AudioSource audioSource;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        enemyHealth = GetComponent<EnemyHealth>();
        enemyCollider = GetComponent<Collider>();

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.spatialBlend = 1f;
        audioSource.playOnAwake = false;
        audioSource.volume = sfxVolume;
        
        agent.updateRotation = true;
        agent.updateUpAxis = true;
    }

    void Start()
    {
        lastPlayerPos = player.position;
        lastEnemyPos = transform.position;

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

        HandleShield();

        if (player == null)
        {
            if (!isRaging)
                Wander();
            return;
        }

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

        if (playerInSight && !wasProvoked)
        {
            wasProvoked = true;
            if (!rageCoroutineRunning)
                StartCoroutine(TriggerRageAndChase());
        }
        else if (wasProvoked && ragePlayed)
        {
            HandleChaseOrAttack(distanceToPlayer);
        }
        else
        {
            if (!isRaging)
                Wander();
        }

        lastPlayerPos = player.position;
        lastEnemyPos = transform.position;
    }

    IEnumerator TriggerRageAndChase()
    {
        rageCoroutineRunning = true;
        isRaging = true;

        agent.isStopped = true;
        ResetAllAnimationStates();
        animator.SetBool("isRage", true); // <-- menggunakan bool

        PlayRageSFX();

        yield return new WaitForSeconds(2.05f); // sesuaikan durasi animasi

        animator.SetBool("isRage", false); // reset setelah selesai
        ragePlayed = true;
        isRaging = false;
        rageCoroutineRunning = false;
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

        SetAnimationState(true, false);
    }

    void HandleChaseOrAttack(float distance)
    {
        if (distance > attackRange)
        {
            agent.isStopped = false;
            agent.speed = chaseSpeed;
            agent.SetDestination(player.position);

            isAttacking = false;
            SetAnimationState(true, false);
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
                PlayAttackSFX();
            }
        }
    }

    public void DealMeleeDamage()
    {
        if (!isAttacking || enemyHealth.shield > 0) return;

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
        if (!wasProvoked)
        {
            wasProvoked = true;
            if (!rageCoroutineRunning)
                StartCoroutine(TriggerRageAndChase());
        }

        if (hurtSFX != null)
            SoundManager.Instance.PlaySound(hurtSFX, 0f, 0f, true, 1f);
    }

    public void DieTrigger()
    {
        if (agent != null)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
        }

        ResetAllAnimationStates();
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

    public void PlayRageSFX()
    {
        if (rageSFX != null)
        {
            SoundManager.Instance.PlaySound(rageSFX, 0f, .1f, true, 1f);
        }
    }

    public void PlayAttackSFX()
    {
        if (attackSFX.Length == 0) return;
        AudioClip clip = attackSFX[Random.Range(0, attackSFX.Length)];
        audioSource.PlayOneShot(clip, sfxVolume);
    }

    private void SetAnimationState(bool isWalking, bool isIdle)
    {
        if (!isRaging)
        {
            animator.SetBool("isWalking", isWalking);
            animator.SetBool("isIdle", isIdle);
        }
    }

    private void ResetAllAnimationStates()
    {
        animator.SetBool("isWalking", false);
        animator.SetBool("isIdle", false);
        animator.SetBool("isRage", false);
        animator.ResetTrigger("attack");
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
