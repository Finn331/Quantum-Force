using cowsins;
using Unity.VisualScripting;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class WaveBreaker : MonoBehaviour
{
    [Header("Wave Effect Settings")]
    [Tooltip("Prefab dari efek visual gelombang suara (misalnya, Particle System).")]
    public GameObject shockwaveVFX;
    [Tooltip("Radius dari gelombang suara yang dihasilkan.")]
    public float shockwaveRadius = 10f;
    [Tooltip("Layer dari objek yang bisa dihancurkan oleh gelombang (tembok, perisai, dll.).")]
    public LayerMask destructibleLayer;

    [Header("Cannonball Settings")]
    [SerializeField] float lifetime;

    [Header("SFX")]
    [Tooltip("Suara yang dimainkan saat bola menabrak tembok.")]
    public AudioClip impactSound;
    [Tooltip("Suara dari gelombang suara itu sendiri.")]
    public AudioClip shockwaveSound;
    private AudioSource audioSource;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.spatialBlend = 1f;
    }

    private void Start()
    {
        Destroy(gameObject, lifetime);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Wall"))
        {
            Debug.Log("WaveBreaker ball hit a Wall!");

            ContactPoint contact = collision.GetContact(0);
            Vector3 position = contact.point;

            if (impactSound != null) AudioSource.PlayClipAtPoint(impactSound, position);

            CreateShockwave(position);

            Destroy(gameObject);
        }
    }

    private void CreateShockwave(Vector3 origin)
    {
        if (shockwaveVFX != null)
        {
            Instantiate(shockwaveVFX, origin, Quaternion.identity);
        }

        if (shockwaveSound != null) AudioSource.PlayClipAtPoint(shockwaveSound, origin);

        // Deteksi semua objek dalam radius yang berada di layer 'destructibleLayer'
        Collider[] objectsInRange = Physics.OverlapSphere(origin, shockwaveRadius, destructibleLayer);

        Debug.Log(objectsInRange.Length + " object(s) with destructible layer detected in shockwave radius.");

        // --- LOGIKA DIPERBARUI ---
        // Cari dan panggil fungsi 'DestroyObject' pada setiap objek yang terdeteksi
        foreach (Collider col in objectsInRange)
        {
            // Coba dapatkan skrip Crate dari objek yang terkena
            Destructible destructibleObject = col.GetComponent<Crate>();
            if (destructibleObject != null)
            {
                // Jika skripnya ada, panggil fungsi Die()
                destructibleObject.Die();
            }
        }
    }
}