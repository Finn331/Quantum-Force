using UnityEngine;

public class BallSpawn : MonoBehaviour
{
    [Header("Ball Spawn Settings")]
    [Tooltip("Titik di mana bola akan muncul.")]
    [SerializeField] Transform spawnPoint;

    [Header("Spawn Control")]
    [Tooltip("Jeda waktu dalam detik sebelum bola lain bisa di-spawn.")]
    [SerializeField] float spawnCooldown = 2.0f;

    private float cooldownTimer = 0f;

    void Update()
    {
        // Hitung mundur cooldown
        if (cooldownTimer > 0)
        {
            cooldownTimer -= Time.deltaTime;
        }
    }

    public void SpawnBall()
    {
        // Cek jika cooldown sudah selesai
        if (cooldownTimer > 0)
        {
            Debug.Log("Masih dalam cooldown.");
            return;
        }

        if (spawnPoint != null)
        {
            // --- LOGIKA UTAMA DIPERBARUI ---
            // Minta satu bola dari Object Pool
            GameObject spawnedBall = ObjectPool.Instance.GetFromPool(spawnPoint.position, Quaternion.identity);

            // Jika pool tidak kosong dan berhasil mendapatkan bola
            if (spawnedBall != null)
            {
                // Mulai cooldown
                cooldownTimer = spawnCooldown;
                Debug.Log("Bola berhasil di-spawn dari pool!");
            }
        }
        else
        {
            Debug.LogWarning("Spawn point belum di-set di inspector.");
        }
    }
}