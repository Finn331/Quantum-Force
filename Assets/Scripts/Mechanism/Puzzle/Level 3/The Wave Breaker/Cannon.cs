using UnityEngine;

public class Cannon : MonoBehaviour
{
    [Header("Cannon Settings")]
    [Tooltip("The initial speed of the cannonball as it leaves the barrel.")]
    [SerializeField] float launchVelocity = 30f;

    // --- VARIABEL BARU ---
    [Tooltip("Adds an extra upward push to create a higher arc. 0 = no extra push, 0.5 = significant arc.")]
    [Range(0f, 1f)]
    [SerializeField] float upwardThrust = 0.25f;

    [Tooltip("The point where the cannonball will spawn.")]
    [SerializeField] Transform cannonBallSpawnPoint;

    [Tooltip("The cannonball prefab to be fired.")]
    [SerializeField] GameObject cannonBallPrefab;

    [Header("Effects")]
    [Tooltip("Particle effect to play at the spawn point upon firing.")]
    [SerializeField] ParticleSystem fireVFX;

    [Tooltip("Sound effect to play upon firing.")]
    [SerializeField] AudioClip fireSFX;

    private AudioSource audioSource;

    private void Awake()
    {
        // Prepare the AudioSource for playing sounds
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 1f; // Make the sound 3D
        }
    }

    public void Launch()
    {
        if (cannonBallPrefab == null || cannonBallSpawnPoint == null)
        {
            Debug.LogError("Cannonball Prefab or Spawn Point is not set!", gameObject);
            return;
        }

        // --- VISUAL AND SOUND EFFECTS ---
        if (fireVFX != null)
        {
            fireVFX.Play();
        }

        if (fireSFX != null)
        {
            audioSource.PlayOneShot(fireSFX);
        }

        // --- NEW LAUNCH LOGIC ---
        GameObject cannonBall = Instantiate(cannonBallPrefab, cannonBallSpawnPoint.position, cannonBallSpawnPoint.rotation);

        Rigidbody rb = cannonBall.GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogError("The cannonball prefab does not have a Rigidbody component!", cannonBall);
            Destroy(cannonBall);
            return;
        }

        // --- PERBAIKAN DI SINI ---
        // 1. Gabungkan arah depan dengan sedikit dorongan ke atas
        Vector3 launchDirection = (cannonBallSpawnPoint.forward + (Vector3.up * upwardThrust)).normalized;

        // 2. Terapkan kecepatan pada arah yang sudah dikombinasikan
        rb.linearVelocity = launchDirection * launchVelocity;
    }
}