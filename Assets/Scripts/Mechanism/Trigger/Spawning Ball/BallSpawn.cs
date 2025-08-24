using UnityEngine;

public class BallSpawn : MonoBehaviour
{
    public enum TagSelectMode { Fixed, Cycle, Random }

    [Header("Ball Spawn Settings")]
    [Tooltip("Titik di mana bola akan muncul.")]
    [SerializeField] private Transform spawnPoint;

    [Header("Pool Tag Settings")]
    [Tooltip("Daftar tag pool yang tersedia (harus cocok dengan tag di ObjectPool kamu).")]
    [SerializeField] private string[] poolTags = new string[] { "BallA", "BallB" };

    [Tooltip("Mode pemilihan tag untuk spawn.")]
    [SerializeField] private TagSelectMode selectMode = TagSelectMode.Fixed;

    [Tooltip("Index tag yang dipakai saat mode Fixed. 0 = poolTags[0].")]
    [SerializeField] private int fixedTagIndex = 0;

    [Header("Spawn Control")]
    [Tooltip("Jeda waktu dalam detik sebelum bola lain bisa di-spawn.")]
    [SerializeField] private float spawnCooldown = 2.0f;

    [Tooltip("Jika true, saat spawn, Rigidbody (jika ada) akan direset velocity-nya.")]
    [SerializeField] private bool resetRigidbodyVelocity = true;

    private float cooldownTimer = 0f;
    private int cycleIndex = 0;

    void Update()
    {
        if (cooldownTimer > 0f)
            cooldownTimer -= Time.deltaTime;
    }

    /// <summary>
    /// Spawn bola menggunakan tag yang dipilih berdasarkan mode (Fixed/Cycle/Random).
    /// </summary>
    public void SpawnBall()
    {
        if (!CanSpawn()) return;

        string tagToUse = PickTagByMode();
        if (string.IsNullOrEmpty(tagToUse))
        {
            Debug.LogWarning("[BallSpawn] Tag kosong/tidak valid.");
            return;
        }

        TrySpawnFromPool(tagToUse);
    }

    /// <summary>
    /// Spawn bola memaksa tag tertentu (mengabaikan mode).
    /// </summary>
    public void SpawnBallWithTag(string tag)
    {
        if (!CanSpawn()) return;

        if (string.IsNullOrEmpty(tag))
        {
            Debug.LogWarning("[BallSpawn] Tag yang dimasukkan kosong.");
            return;
        }

        TrySpawnFromPool(tag);
    }

    /// <summary>
    /// Ubah tag tetap (untuk mode Fixed) via index.
    /// </summary>
    public void SetFixedTagIndex(int index)
    {
        if (poolTags == null || poolTags.Length == 0)
        {
            Debug.LogWarning("[BallSpawn] poolTags kosong.");
            return;
        }
        fixedTagIndex = Mathf.Clamp(index, 0, poolTags.Length - 1);
    }

    /// <summary>
    /// Ubah tag tetap (untuk mode Fixed) via nama tag.
    /// </summary>
    public void SetFixedTag(string tag)
    {
        if (poolTags == null || poolTags.Length == 0) return;
        int idx = System.Array.IndexOf(poolTags, tag);
        if (idx >= 0) fixedTagIndex = idx;
        else Debug.LogWarning($"[BallSpawn] Tag '{tag}' tidak ada di poolTags.");
    }

    /// <summary>
    /// Ubah mode pemilihan tag.
    /// </summary>
    public void SetSelectMode(TagSelectMode mode) => selectMode = mode;

    // -------------------- Helper --------------------

    private bool CanSpawn()
    {
        if (cooldownTimer > 0f)
        {
            Debug.Log("[BallSpawn] Masih dalam cooldown.");
            return false;
        }

        if (spawnPoint == null)
        {
            Debug.LogWarning("[BallSpawn] Spawn point belum di-set di inspector.");
            return false;
        }

        if (ObjectPool.Instance == null)
        {
            Debug.LogError("[BallSpawn] ObjectPool.Instance tidak ditemukan.");
            return false;
        }

        return true;
    }

    private string PickTagByMode()
    {
        if (poolTags == null || poolTags.Length == 0) return null;

        switch (selectMode)
        {
            case TagSelectMode.Fixed:
                fixedTagIndex = Mathf.Clamp(fixedTagIndex, 0, poolTags.Length - 1);
                return poolTags[fixedTagIndex];

            case TagSelectMode.Cycle:
                if (cycleIndex >= poolTags.Length) cycleIndex = 0;
                string t = poolTags[cycleIndex];
                cycleIndex = (cycleIndex + 1) % poolTags.Length;
                return t;

            case TagSelectMode.Random:
                int r = Random.Range(0, poolTags.Length);
                return poolTags[r];
        }
        return null;
    }

    private void TrySpawnFromPool(string tagToUse)
    {
        // Pastikan ObjectPool kamu punya API seperti ini:
        // GameObject GetFromPool(string tag, Vector3 position, Quaternion rotation)
        // Jika API kamu berbeda, sesuaikan satu baris di bawah ini.
        GameObject spawned = ObjectPool.Instance.GetFromPool(tagToUse, spawnPoint.position, spawnPoint.rotation);

        if (spawned != null)
        {
            if (resetRigidbodyVelocity)
            {
                var rb = spawned.GetComponent<Rigidbody>();
                if (rb != null)
                {
#if UNITY_6000_0_OR_NEWER
                    rb.linearVelocity = Vector3.zero;
#else
                    rb.velocity = Vector3.zero;
#endif
                    rb.angularVelocity = Vector3.zero;
                }
            }

            cooldownTimer = spawnCooldown;
            Debug.Log($"[BallSpawn] Spawned '{tagToUse}' dari pool.");
        }
        else
        {
            Debug.LogWarning($"[BallSpawn] Pool mengembalikan null untuk tag '{tagToUse}'. Pastikan pool & tag sudah benar.");
        }
    }
}
