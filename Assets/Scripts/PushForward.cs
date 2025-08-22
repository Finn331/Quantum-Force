using UnityEngine;

public class PushForward : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] float pushForce;

    private GameObject ball;    

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ball"))
        {
            ball = collision.gameObject;
            Rigidbody rb = ball.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.AddForce(transform.forward * pushForce, ForceMode.Impulse);
            }
        }
    }
    // tes
}

