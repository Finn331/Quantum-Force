using UnityEngine;

public class CannonBallCollision : MonoBehaviour
{
    [Header("Metal Wall Settings")]
    [Tooltip("Tag untuk tembok besi yang harus dipukul oleh cannonball.")]
    [SerializeField] private string metalWallTag = "MetalWall";

    [Header("Misc")]
    [Tooltip("Delay sebelum cannonball dihancurkan setelah tabrakan.")]
    [SerializeField] private float destroyDelayAfterHit = 0.05f;

    // BOS DISIMPAN SECARA PRIVATE, DISETUP DARI CANNON
    private Singulra singulra;

    /// <summary>
    /// Dipanggil dari Cannon setelah cannonball di-Instantiate.
    /// </summary>
    public void Setup(Singulra boss)
    {
        singulra = boss;
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Pastikan yang ditabrak adalah tembok besi
        if (collision.collider.CompareTag(metalWallTag))
        {
            if (singulra != null)
            {
                // Minta boss hancurkan shield khusus Phase 2B
                singulra.BreakPhase2BShieldByMetalHit();
            }
            else
            {
                Debug.LogWarning("CannonBallCollision: Singulra reference is null. Shield won't break.");
            }

            // Hancurkan cannonball agar tidak mantul-mantul terus
            Destroy(gameObject, destroyDelayAfterHit);
        }
    }
}
