using UnityEngine;

public class SlipperyFloor : MonoBehaviour
{
    [SerializeField] string tagName;
    public float slipForce = 5f;

    void OnTriggerStay(Collider other)
    {
        if (other.CompareTag(tagName))
        {
            Rigidbody rb = other.GetComponent<Rigidbody>();
            if (rb != null)
            {
                Vector3 slipDir = rb.linearVelocity.normalized;
                rb.AddForce(slipDir * slipForce, ForceMode.Acceleration);
            }
        }
    }
}

