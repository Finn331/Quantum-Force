using cowsins;
using UnityEngine;

public class GlassBreak : MonoBehaviour
{
    public enum DetectionMode
    {
        TriggerOnly,
        CollisionOnly,
        Both
    }

    [Header("Glass Setting")]
    [SerializeField] private string tagToBreak;     // untuk trigger
    [SerializeField] private string collisionTag; // untuk collision
    [SerializeField] private DetectionMode detectionMode = DetectionMode.Both;

    // Script Reference
    private Crate crate;

    void Start()
    {
        crate = GetComponent<Crate>();
        if (crate == null)
        {
            Debug.LogError("GlassBreak membutuhkan komponen Crate pada GameObject ini.");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (detectionMode == DetectionMode.TriggerOnly || detectionMode == DetectionMode.Both)
        {
            if (other.CompareTag(tagToBreak))
            {
                crate.Die();
            }
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (detectionMode == DetectionMode.CollisionOnly || detectionMode == DetectionMode.Both)
        {
            if (collision.gameObject.CompareTag(collisionTag))
            {
                crate.Die();
            }
        }
    }
}
