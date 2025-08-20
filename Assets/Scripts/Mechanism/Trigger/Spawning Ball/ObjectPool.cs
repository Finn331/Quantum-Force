using System.Collections.Generic;
using UnityEngine;

public class ObjectPool : MonoBehaviour
{
    // Singleton pattern agar mudah diakses dari mana saja
    public static ObjectPool Instance;

    [Header("Pool Settings")]
    [Tooltip("Drag semua GameObject bola yang sudah Anda siapkan di Hierarchy ke dalam list ini.")]
    [SerializeField] private List<GameObject> prewarmedObjects; // Menggantikan prefab dan poolSize

    // "Kotak" untuk menyimpan objek yang tidak aktif
    private Queue<GameObject> objectPool = new Queue<GameObject>();

    private void Awake()
    {
        // Setup Singleton
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        // --- LOGIKA UTAMA DIPERBARUI ---
        // Alih-alih membuat objek baru, kita ambil dari list yang sudah ada
        foreach (GameObject obj in prewarmedObjects)
        {
            if (obj != null)
            {
                obj.SetActive(false); // Sembunyikan objek
                objectPool.Enqueue(obj); // Masukkan ke dalam "kotak"
            }
        }
    }

    /// <summary>
    /// Mengambil satu objek dari pool.
    /// </summary>
    public GameObject GetFromPool(Vector3 position, Quaternion rotation)
    {
        if (objectPool.Count == 0)
        {
            Debug.LogWarning("Object Pool is empty.");
            return null;
        }

        GameObject obj = objectPool.Dequeue();

        obj.transform.position = position;
        obj.transform.rotation = rotation;

        obj.SetActive(true);

        return obj;
    }

    /// <summary>
    /// Mengembalikan objek ke dalam pool.
    /// </summary>
    public void ReturnToPool(GameObject obj)
    {
        obj.SetActive(false);
        objectPool.Enqueue(obj);
    }
}