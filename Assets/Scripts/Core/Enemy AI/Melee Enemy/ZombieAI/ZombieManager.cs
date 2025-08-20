using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events; // Pastikan ini ada
using System.Collections; // Diperlukan untuk Coroutine

public class ZombieManager : MonoBehaviour
{
    //public static ZombieManager Instance;

    [SerializeField] private List<ZombieAI> allZombies = new List<ZombieAI>();

    [Header("Alert Settings")]
    [SerializeField] private float alertRadius = 15f;
    [SerializeField] private int maxAlertCount = 4;

    [Header("Zombie Tracking")]
    private int initialZombieCount;
    [SerializeField] int zombiesKilled = 0;

    [Header("Custom Events")]
    public int killCountForEvent = 5;
    public UnityEvent onKillCountReached;
    public UnityEvent onAllZombiesKilled;

    [Header("Destruction Settings")]
    [Tooltip("Objek yang akan dihancurkan saat event dipicu.")]
    [SerializeField] GameObject piggyfrogManagerGameobject;
    [Tooltip("Waktu jeda sebelum objek dihancurkan (dalam detik).")]
    [SerializeField] float destructionDelay = 3.0f; // Variabel baru untuk jeda

    public int InitialZombieCount => initialZombieCount;
    public int ZombiesKilled => zombiesKilled;
    public int ZombiesRemaining => allZombies.Count;

    void Awake()
    {
        //if (Instance == null)
        //    Instance = this;
        //else
        //    Destroy(gameObject);
    }

    void Start()
    {
        initialZombieCount = allZombies.Count;
    }

    public void RegisterZombie(ZombieAI zombie)
    {
        if (!allZombies.Contains(zombie))
        {
            allZombies.Add(zombie);
            initialZombieCount = allZombies.Count;
        }
    }

    public void UnregisterZombie(ZombieAI zombie)
    {
        if (allZombies.Contains(zombie))
        {
            allZombies.Remove(zombie);
            zombiesKilled++;
            Debug.Log("Zombie mati! Total mati: " + zombiesKilled);
            CheckForEvents();
        }
    }

    private void CheckForEvents()
    {
        if (zombiesKilled == killCountForEvent)
        {
            Debug.Log(killCountForEvent + " zombie telah dikalahkan! Memicu event...");
            onKillCountReached.Invoke();
        }

        if (allZombies.Count == 0)
        {
            Debug.Log("Semua zombie telah dikalahkan! Memicu event terakhir...");
            onAllZombiesKilled.Invoke();
        }
    }

    public void AlertNearbyZombies(Vector3 position)
    {
        int alerted = 0;
        foreach (var zombie in allZombies)
        {
            if (zombie == null || zombie.IsProvoked) continue;

            float distance = Vector3.Distance(zombie.transform.position, position);
            if (distance <= alertRadius)
            {
                zombie.ReceiveLocalAlert();
                alerted++;

                if (alerted >= maxAlertCount)
                    break;
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, alertRadius);
    }

    // --- FUNGSI DIPERBAIKI ---
    // Fungsi ini yang Anda panggil dari UnityEvent
    public void DestroySelf()
    {
        // Memulai Coroutine dengan jeda waktu yang sudah ditentukan
        StartCoroutine(DestroyAfterDelay(destructionDelay));
    }

    // Coroutine yang menangani jeda waktu
    private IEnumerator DestroyAfterDelay(float delay)
    {
        Debug.Log("Penghancuran akan dimulai dalam " + delay + " detik...");
        // Tunggu selama 'delay' detik
        yield return new WaitForSeconds(delay);

        // Setelah menunggu, baru hancurkan objeknya
        if (piggyfrogManagerGameobject != null)
        {
            Debug.Log("Menghancurkan " + piggyfrogManagerGameobject.name);
            Destroy(piggyfrogManagerGameobject);
        }
        else
        {
            Debug.LogWarning("PiggyfrogManager GameObject tidak ditemukan untuk dihancurkan!");
        }
    }
}