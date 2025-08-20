using UnityEngine;
using cowsins; // Tambahkan ini jika PlayerStats ada di dalam namespace cowsins

// Pastikan objek ini selalu memiliki Collider
[RequireComponent(typeof(Collider))]
public class VoidKillTrigger : MonoBehaviour
{
    [Header("Pengaturan Zona Kematian")]
    [Tooltip("Jumlah damage yang diberikan. Atur sangat tinggi untuk memastikan kematian instan.")]
    [SerializeField] private float lethalDamage = 99999f;

    [Header("Efek (Opsional)")]
    [Tooltip("Suara yang akan diputar saat pemain menyentuh zona ini.")]
    [SerializeField] private AudioClip deathSound;

    private void Awake()
    {
        // Memastikan collider pada objek ini diatur sebagai Trigger secara otomatis
        GetComponent<Collider>().isTrigger = true;
    }

    // Fungsi ini akan terpanggil saat ada objek lain masuk ke dalam trigger
    private void OnTriggerEnter(Collider other)
    {
        // Cek apakah objek yang masuk memiliki tag "Player"
        if (other.CompareTag("Player"))
        {
            Debug.Log("Player memasuki zona kematian!");

            // Coba dapatkan komponen PlayerStats dari objek pemain
            PlayerStats stats = other.GetComponent<PlayerStats>();

            // Jika komponen PlayerStats ditemukan
            if (stats != null)
            {
                // Putar suara kematian di lokasi pemain jika ada
                if (deathSound != null)
                {
                    AudioSource.PlayClipAtPoint(deathSound, other.transform.position);
                }

                // Berikan damage yang mematikan ke pemain
                stats.Damage(lethalDamage, false);
            }
            else
            {
                Debug.LogWarning("Objek 'Player' tidak memiliki komponen PlayerStats!", other.gameObject);
            }
        }
    }
}