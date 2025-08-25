using UnityEngine;
using UnityEngine.Events; // Diperlukan untuk UnityEvent

// Pastikan objek ini memiliki Collider
[RequireComponent(typeof(Collider))]
public class MultiObjectTrigger : MonoBehaviour
{
    [Header("Pengaturan Pemicu")]
    [Tooltip("Tag dari objek yang akan memicu event ini (misalnya, 'Player').")]
    public string triggerTag = "Player";

    [Header("Objek yang Dikontrol")]
    [Tooltip("Masukkan semua GameObject yang ingin diaktifkan saat trigger disentuh.")]
    public GameObject[] objectsToEnable;

    [Tooltip("Masukkan semua GameObject yang ingin dinonaktifkan saat trigger disentuh.")]
    public GameObject[] objectsToDisable;

    [Header("Event Tambahan (Opsional)")]
    [Tooltip("Fungsi tambahan yang akan dipicu saat trigger diaktifkan.")]
    public UnityEvent onTriggerActivated;

    [Header("Pengaturan Sekali Pakai")]
    [Tooltip("Centang ini jika trigger hanya bisa digunakan satu kali.")]
    public bool triggerOnce = true;

    private bool hasBeenTriggered = false;

    private void Awake()
    {
        // Pastikan collider diatur sebagai trigger
        GetComponent<Collider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        // Cek jika sudah pernah terpicu dan hanya boleh sekali
        if (triggerOnce && hasBeenTriggered)
        {
            return;
        }

        // Cek apakah yang masuk memiliki tag yang benar
        if (other.CompareTag(triggerTag))
        {
            Debug.Log(triggerTag + " memasuki trigger. Mengaktifkan aksi...");

            // Aktifkan semua objek di daftar 'objectsToEnable'
            foreach (GameObject obj in objectsToEnable)
            {
                if (obj != null) obj.SetActive(true);
            }

            // Nonaktifkan semua objek di daftar 'objectsToDisable'
            foreach (GameObject obj in objectsToDisable)
            {
                if (obj != null) obj.SetActive(false);
            }

            // Panggil event tambahan jika ada
            onTriggerActivated.Invoke();

            // Tandai bahwa trigger ini sudah digunakan
            hasBeenTriggered = true;

            // Destroy diri sendiri setelah 2 detik
            Destroy(gameObject, 2f);
        }
    }
}
