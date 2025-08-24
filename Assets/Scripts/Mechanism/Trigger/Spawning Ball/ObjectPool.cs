using System.Collections.Generic;
using UnityEngine;

public class ObjectPool : MonoBehaviour
{
    public static ObjectPool Instance;

    [Header("Pool Settings")]
    [Tooltip("Drag semua GameObject yang sudah ada di scene (prewarmed).")]
    [SerializeField] private List<GameObject> prewarmedObjects = new List<GameObject>();

    // Pool disimpan per-tag
    private readonly Dictionary<string, Queue<GameObject>> poolsByTag = new Dictionary<string, Queue<GameObject>>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        poolsByTag.Clear();

        foreach (var obj in prewarmedObjects)
        {
            if (obj == null) continue;

            // Ambil tag dari object (mis. "BallA", "BallB")
            string tag = string.IsNullOrEmpty(obj.tag) ? "Untagged" : obj.tag;

            if (!poolsByTag.TryGetValue(tag, out var q))
            {
                q = new Queue<GameObject>();
                poolsByTag[tag] = q;
            }

            obj.SetActive(false);
            q.Enqueue(obj);
        }
    }

    /// <summary>
    /// Ambil satu object dari pool berdasarkan tag (versi baru, modular).
    /// </summary>
    public GameObject GetFromPool(string tag, Vector3 position, Quaternion rotation)
    {
        if (string.IsNullOrEmpty(tag))
        {
            Debug.LogWarning("[ObjectPool] Tag kosong. Gunakan overload tanpa tag atau berikan tag yang valid.");
            return null;
        }

        if (!poolsByTag.TryGetValue(tag, out var q) || q.Count == 0)
        {
            Debug.LogWarning($"[ObjectPool] Pool untuk tag '{tag}' kosong / belum disiapkan.");
            return null;
        }

        var obj = q.Dequeue();
        ActivateAt(obj, position, rotation);
        return obj;
    }

    /// <summary>
    /// Overload lama: ambil dari pool mana saja yang masih punya stok.
    /// </summary>
    public GameObject GetFromPool(Vector3 position, Quaternion rotation)
    {
        // Cari antrian yang masih punya objek
        foreach (var kv in poolsByTag)
        {
            var q = kv.Value;
            if (q.Count > 0)
            {
                var obj = q.Dequeue();
                ActivateAt(obj, position, rotation);
                return obj;
            }
        }

        Debug.LogWarning("[ObjectPool] Semua pool kosong.");
        return null;
    }

    /// <summary>
    /// Kembalikan object ke pool sesuai tag object-nya.
    /// </summary>
    public void ReturnToPool(GameObject obj)
    {
        if (obj == null) return;

        string tag = string.IsNullOrEmpty(obj.tag) ? "Untagged" : obj.tag;

        if (!poolsByTag.TryGetValue(tag, out var q))
        {
            q = new Queue<GameObject>();
            poolsByTag[tag] = q;
        }

        obj.SetActive(false);
        q.Enqueue(obj);
    }

    private static void ActivateAt(GameObject obj, Vector3 pos, Quaternion rot)
    {
        var t = obj.transform;
        t.position = pos;
        t.rotation = rot;
        obj.SetActive(true);
    }
}
