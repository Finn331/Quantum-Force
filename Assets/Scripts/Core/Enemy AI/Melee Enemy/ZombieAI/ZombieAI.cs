using cowsins;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class ZombieAI : MonoBehaviour
{
    [Header("Animator")]
    public Animator mainAnimator;
    public Animator lodAnimator;

    [Header("AI Components")]
    public NavMeshAgent agent;
    public Transform target;
    public float meleeDamage; // Atur nilai damage di Inspector

    [Header("Wandering")]
    public List<Transform> waypoints;
    private int waypointIndex = 0;
    public float waypointWaitTime = 2f;
    private float waitTimer = 0f;

    [Header("Detection Range")]
    public float chaseRange = 10f;
    public float attackRange = 2f;

    [Header("Attack Setting")]
    public float attackCooldown = 1.5f;
    private float nextAttackTime = 0f;

    [Header("Rage Settings")]
    public float rageDuration = 1.5f;
    private bool isRaging = false;
    private float rageTimer = 0f;

    [Header("Health")]
    public EnemyHealth enemyHealth;
    [SerializeField] float currentHP;

    private bool isProvoked = false;
    private bool hasDied = false;

    [Header("Raycast Settings")]
    public float raycastHeightOffset = 1.2f;
    public LayerMask obstacleLayer;

    [Header("Audio Clip SFX")]
    [SerializeField] AudioClip rageSFX;
    [SerializeField] AudioClip dieSFX;
    [SerializeField] AudioClip hurtSFX;

    [Header("Script References")]
    [SerializeField] ZombieManager zombieManager;

    private float lastRageSfxTime = -1f;

    void Start()
    {
        currentHP = enemyHealth.health;
        if (agent == null) agent = GetComponent<NavMeshAgent>();
        if (target == null) target = GameObject.FindGameObjectWithTag("Player")?.transform;

        //ZombieManager.Instance?.RegisterZombie(this);

        if (waypoints != null && waypoints.Count > 0)
        {
            agent.SetDestination(waypoints[waypointIndex].position);
        }
    }

    void Update()
    {
        if (hasDied) return;

        if (enemyHealth.health <= 0)
        {
            HandleDeath();
            return;
        }

        if (target == null)
        {
            HandleWanderingState();
            return;
        }

        float distanceToPlayer = Vector3.Distance(target.position, transform.position);
        bool isDamaged = enemyHealth.health < currentHP;

        if (isDamaged)
        {
            HurtSFX();
        }

        if (!isProvoked)
        {
            if (isDamaged)
            {
                isProvoked = true;
                if (zombieManager != null)
                {
                    zombieManager.AlertNearbyZombies(transform.position);
                }
                else
                {
                    Debug.LogWarning("ZombieManager is not assigned in ZombieAI script on " + gameObject.name);
                }
            }
            else if (distanceToPlayer <= chaseRange && HasLineOfSightToPlayer())
            {
                Provoke();
            }
        }

        currentHP = enemyHealth.health;

        if (isProvoked)
        {
            HandleProvokedState(distanceToPlayer);
        }
        else
        {
            HandleWanderingState();
        }
    }

    private void HandleDeath()
    {
        hasDied = true;
        SetAnimBool("isIdle", false);
        SetAnimBool("isWalking", false);
        SetAnimBool("isRunning", false);
        SetAnimBool("isDead", true);
        DieSFX();
        if (agent.enabled)
        {
            agent.isStopped = true;
            agent.enabled = false;
        }
        GetComponent<Collider>().enabled = false;

        if (zombieManager != null)
        {
            zombieManager.UnregisterZombie(this);
        }
        else
        {
            Debug.LogWarning("ZombieManager is not assigned in ZombieAI script on " + gameObject.name);
        }
    }

    private void HandleProvokedState(float distanceToPlayer)
    {
        SetAnimBool("isIdle", false);
        if (isRaging)
        {
            HandleRageState();
            agent.isStopped = true;
            SetAnimBool("isWalking", false);
            SetAnimBool("isRunning", false);
            return;
        }
        if (distanceToPlayer <= attackRange && HasLineOfSightToPlayer())
        {
            HandleAttack();
        }
        else
        {
            HandleChase();
        }
    }

    private void HandleWanderingState()
    {
        SetAnimBool("isRunning", false);
        if (waypoints == null || waypoints.Count == 0)
        {
            agent.isStopped = true;
            SetAnimBool("isWalking", false);
            SetAnimBool("isIdle", true);
            return;
        }
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            waitTimer += Time.deltaTime;
            SetAnimBool("isWalking", false);
            SetAnimBool("isIdle", true);
            if (waitTimer >= waypointWaitTime)
            {
                waitTimer = 0f;
                waypointIndex = (waypointIndex + 1) % waypoints.Count;
                agent.SetDestination(waypoints[waypointIndex].position);
            }
        }
        else
        {
            agent.isStopped = false;
            SetAnimBool("isWalking", true);
            SetAnimBool("isIdle", false);
        }
    }

    private void HandleRageState()
    {
        if (isRaging)
        {
            rageTimer -= Time.deltaTime;
            if (rageTimer <= 0f)
            {
                isRaging = false;
                SetAnimBool("isRage", false);
            }
        }
    }

    private void HandleAttack()
    {
        agent.isStopped = true;
        SetAnimBool("isIdle", false);
        SetAnimBool("isWalking", false);
        SetAnimBool("isRunning", false);
        FaceTarget();
        if (Time.time >= nextAttackTime)
        {
            TriggerAttack();
            nextAttackTime = Time.time + attackCooldown;
        }
    }

    private void HandleChase()
    {
        agent.isStopped = false;
        agent.SetDestination(target.position);
        SetAnimBool("isIdle", false);
        SetAnimBool("isWalking", false);
        SetAnimBool("isRunning", true);
    }

    private void Provoke()
    {
        if (isProvoked) return;
        isProvoked = true;
        StartRage();

        if (zombieManager != null)
        {
            zombieManager.AlertNearbyZombies(transform.position);
        }
        else
        {
            Debug.LogWarning("ZombieManager is not assigned in ZombieAI script on " + gameObject.name);
        }
    }

    public void StartRage()
    {
        if (isRaging) return;
        isRaging = true;
        rageTimer = rageDuration;
        SetAnimBool("isRage", true);
    }

    public void ReceiveLocalAlert()
    {
        if (!isProvoked)
        {
            isProvoked = true;
            StartRage();
        }
    }

    public bool IsProvoked => isProvoked;

    private void FaceTarget()
    {
        Vector3 direction = (target.position - transform.position).normalized;
        Quaternion lookRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 5f);
    }

    private void SetAnimBool(string param, bool value)
    {
        if (mainAnimator != null) mainAnimator.SetBool(param, value);
        if (lodAnimator != null) lodAnimator.SetBool(param, value);
    }

    private void TriggerAttack()
    {
        if (enemyHealth.health > 100f)
        {
            if (mainAnimator != null) mainAnimator.SetTrigger("attack2");
            if (lodAnimator != null) lodAnimator.SetTrigger("attack2");
        }
        else
        {
            if (mainAnimator != null) mainAnimator.SetTrigger("attack");
            if (lodAnimator != null) lodAnimator.SetTrigger("attack");
        }
    }

    private bool HasLineOfSightToPlayer()
    {
        if (target == null) return false;
        Vector3 origin = transform.position + Vector3.up * raycastHeightOffset;
        Vector3 dirToPlayer = (target.position - origin).normalized;
        float distance = Vector3.Distance(origin, target.position);
        if (Physics.Raycast(origin, dirToPlayer, out RaycastHit hit, distance, obstacleLayer))
        {
            Debug.DrawLine(origin, hit.point, Color.red);
            return false;
        }
        Debug.DrawLine(origin, origin + dirToPlayer * distance, Color.green);
        return true;
    }

    // --- SFX HANDLER ---
    public void PlayRageSFX()
    {
        if (Time.time - lastRageSfxTime < 0.1f) return;
        lastRageSfxTime = Time.time;
        if (rageSFX != null) SoundManager.Instance.PlaySound(rageSFX, 0, 0, true, 0);
    }
    void DieSFX()
    {
        if (dieSFX != null) SoundManager.Instance.PlaySound(dieSFX, 0f, 0f, true, 1f);
    }
    void HurtSFX()
    {
        if (hurtSFX != null) SoundManager.Instance.PlaySound(hurtSFX, 0f, 0f, true, 1f);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("FallenObject"))
        {
            enemyHealth.health = 0;
        }
    }

    // --- FUNGSI BARU UNTUK MEMBERI DAMAGE ---
    // Panggil fungsi ini dari Animation Event pada klip animasi serangan Anda
    public void DealDamage()
    {
        // Cek jika target masih dalam jangkauan saat animasi serangan mengenai
        if (target != null && Vector3.Distance(transform.position, target.position) <= attackRange)
        {
            // Coba dapatkan komponen kesehatan pemain (ganti 'PlayerStats' jika perlu)
            PlayerStats playerStats = target.GetComponent<PlayerStats>();
            if (playerStats != null)
            {
                // Berikan damage ke pemain
                playerStats.Damage(meleeDamage, false);
                Debug.Log(gameObject.name + " menyerang pemain sebesar " + meleeDamage + " damage!");
            }
        }
    }
}