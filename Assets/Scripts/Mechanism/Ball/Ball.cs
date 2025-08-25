using UnityEngine;
using System.Linq;

public class Ball : MonoBehaviour
{
    [Header("Bounce Settings")]
    [Tooltip("Akan bertambah setiap kali bola memantul pada tag di 'Bounce Tags'.")]
    public int currBounce;

    [Tooltip("Daftar tag yang memicu pantulan & penambahan currBounce.")]
    [SerializeField] private string[] bounceTags = new[] { "Wall" };

    [Tooltip("Pengali kecepatan setelah memantul. 1 = energi sama, <1 = kehilangan energi, >1 = tambah energi.")]
    [SerializeField, Range(0f, 2f)] private float bounceSpeedMultiplier = 1f;

    [Tooltip("Jika kecepatan terlalu kecil, paksa minimal ini setelah pantulan agar tidak macet.")]
    [SerializeField] private float minPostBounceSpeed = 0.5f;

    [Header("Other")]
    [Tooltip("Tag yang akan menghancurkan bola saat bertabrakan.")]
    [SerializeField] private string voidTag = "Void";

    private Rigidbody rb;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogError("[Ball] Rigidbody component is missing from the Ball object.", this);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (rb == null || collision.contactCount == 0) return;

        string hitTag = collision.gameObject.tag;

        // Hancurkan saat menyentuh void
        if (!string.IsNullOrEmpty(voidTag) && hitTag == voidTag)
        {
            Destroy(gameObject);
            return;
        }

        // Pantul + tambah currBounce jika tag ada di daftar bounceTags
        if (bounceTags != null && bounceTags.Contains(hitTag))
        {
            Vector3 incomingVel = rb.linearVelocity; // pakai linearVelocity
            if (incomingVel.sqrMagnitude < 0.0001f)
                incomingVel = -collision.contacts[0].normal * minPostBounceSpeed;

            Vector3 normal = collision.contacts[0].normal;
            Vector3 bounceDirection = Vector3.Reflect(incomingVel.normalized, normal);

            float newSpeed = Mathf.Max(incomingVel.magnitude * bounceSpeedMultiplier, minPostBounceSpeed);
            rb.linearVelocity = bounceDirection * newSpeed; // pakai linearVelocity

            currBounce++;
        }
    }
}
