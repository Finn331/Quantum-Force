using UnityEngine;

public class Ball : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] float pushForce;

    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        PushForce();
    }
 
    void PushForce()
    {
        rb.AddForce(transform.forward * pushForce, ForceMode.Impulse);
    }
}
