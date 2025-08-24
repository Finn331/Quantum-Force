using UnityEngine;

public class PooledBall : MonoBehaviour
{
    [Header("Lifetime Settings")]
    [Tooltip("Waktu dalam detik sebelum bola ini otomatis kembali ke pool.")]
    public float lifetime = 7.0f;

    // --- FITUR BARU ---
    [Header("Collision Settings")]
    [Tooltip("Tag dari objek yang akan membuat bola ini 'hancur' dan kembali ke pool.")]
    public string targetTag = "Wall"; // Anda bisa ganti ini di Inspector

    [Header("Script Reference")]
    [SerializeField] PickupSystem pickupSystem; // Referensi ke sistem pengambilan objek

    private float collisionGracePeriod = 0.2f;
    private bool canCollide = false;

    private void OnEnable()
    {
        canCollide = false;
        StartCoroutine(ActivateBall());
    }

    private System.Collections.IEnumerator ActivateBall()
    {
        yield return new WaitForSeconds(collisionGracePeriod);
        canCollide = true;

        // Hitungan mundur lifetime tetap ada sebagai cadangan
        yield return new WaitForSeconds(lifetime - collisionGracePeriod);

        if (gameObject.activeInHierarchy)
        {
            ReturnToPool();
            pickupSystem.ThrowObject(); // Panggil sistem pengambilan objek
        }
    }

    // --- LOGIKA ONCOLLISIONENTER DIPERBARUI ---
    private void OnCollisionEnter(Collision collision)
    {
        // Abaikan tabrakan jika masih dalam masa tenggang
        if (!canCollide) return;

        // Cek apakah objek yang ditabrak memiliki tag yang benar
        if (collision.gameObject.CompareTag(targetTag))
        {
            Debug.Log("Bola mengenai target: " + collision.gameObject.name);
            // Jika ya, baru kembalikan ke pool
            ReturnToPool();
        }
        // Jika tidak, bola akan memantul dan melanjutkan perjalanannya seperti biasa.
    }

    private void ReturnToPool()
    {
        StopAllCoroutines();
        ObjectPool.Instance.ReturnToPool(gameObject);
    }
}