using UnityEngine;
using cowsins;

public class Checkpoint : MonoBehaviour
{
    [Header("Checkpoint Settings")]
    [Tooltip("Jika true, akan menampilkan notifikasi di console/UI saat checkpoint tersimpan")]
    [SerializeField] private bool showNotification = true;

    [Header("References")]
    [Tooltip("Optional: Sound effect saat checkpoint aktif")]
    [SerializeField] private AudioClip checkpointReachedSFX;

    private bool isActivated = false;

    private void OnTriggerEnter(Collider other)
    {
        // Cek jika yang masuk adalah player (bisa cek tag "Player" atau komponen PlayerStats)
        if (!isActivated && other.CompareTag("Player"))
        {
            ActivateCheckpoint(other);
        }
    }

    private void ActivateCheckpoint(Collider player)
    {
        isActivated = true;

        if (showNotification)
        {
            Debug.Log("Checkpoint Reached!");
        }

        if (checkpointReachedSFX != null)
        {
            AudioSource.PlayClipAtPoint(checkpointReachedSFX, transform.position);
        }

        // 1. Update posisi respawn player saat ini
        PlayerStats stats = player.GetComponent<PlayerStats>();
        if (stats != null)
        {
            stats.respawnPosition = transform.position;
        }

        // 2. Simpan data permanen ke file
        cowsins.WeaponController weaponController = player.GetComponent<cowsins.WeaponController>();

        // Pass to SaveManager (Method overload needs to be added in SaveManager)
        SaveManager.instance.SaveGame(stats ? stats.respawnPosition : transform.position, UnityEngine.SceneManagement.SceneManager.GetActiveScene().name, weaponController);
    }
}
