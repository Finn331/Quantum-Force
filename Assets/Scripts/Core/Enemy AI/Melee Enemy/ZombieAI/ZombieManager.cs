using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events; // Tambahkan ini untuk menggunakan UnityEvent

public class ZombieManager : MonoBehaviour
{
    public static ZombieManager Instance;

    // Pastikan Anda mengisi list ini dari Inspector sebelum memulai game
    [SerializeField] private List<ZombieAI> allZombies = new List<ZombieAI>();

    [Header("Alert Settings")]
    [SerializeField] private float alertRadius = 15f;
    [SerializeField] private int maxAlertCount = 4;

    [Header("Zombie Tracking")]
    // Variabel untuk melacak jumlah zombie
    private int initialZombieCount;
    [SerializeField] int zombiesKilled = 0;

    [Header("Custom Events")]
    public int killCountForEvent = 5; // Jumlah kill yang dibutuhkan untuk event pertama
    public UnityEvent onKillCountReached; // Event yang dipicu saat kill mencapai 'killCountForEvent'
    public UnityEvent onAllZombiesKilled; // Event yang dipicu saat semua zombie mati

    // Properti publik agar skrip lain bisa membaca data ini dengan aman
    public int InitialZombieCount => initialZombieCount;
    public int ZombiesKilled => zombiesKilled;
    public int ZombiesRemaining => allZombies.Count;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    void Start()
    {
        // Catat jumlah zombie awal berdasarkan yang ada di list
        initialZombieCount = allZombies.Count;
    }

    // Fungsi ini tidak lagi diperlukan jika Anda mengisi list dari Inspector,
    // tapi biarkan saja jika Anda ingin menambah zombie saat runtime.
    public void RegisterZombie(ZombieAI zombie)
    {
        if (!allZombies.Contains(zombie))
        {
            allZombies.Add(zombie);
            // Update jumlah awal jika ada zombie yang mendaftar saat runtime
            initialZombieCount = allZombies.Count;
        }
    }

    public void UnregisterZombie(ZombieAI zombie)
    {
        if (allZombies.Contains(zombie))
        {
            allZombies.Remove(zombie);
            zombiesKilled++; // Tambah hitungan zombie yang mati

            Debug.Log("Zombie mati! Total mati: " + zombiesKilled);

            // Cek apakah ada event yang perlu dipicu
            CheckForEvents();
        }
    }

    private void CheckForEvents()
    {
        // Cek apakah jumlah kill sudah mencapai target untuk event pertama
        if (zombiesKilled == killCountForEvent)
        {
            Debug.Log(killCountForEvent + " zombie telah dikalahkan! Memicu event...");
            onKillCountReached.Invoke();
        }

        // Cek apakah semua zombie sudah dikalahkan
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
}