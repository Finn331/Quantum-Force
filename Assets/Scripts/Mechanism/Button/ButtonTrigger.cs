using UnityEngine;
using UnityEngine.Events;

public class ButtonTrigger : MonoBehaviour
{
    [Header("Event")]
    [Tooltip("Fungsi yang akan dipicu SETELAH animasi Button selesai.")]
    public UnityEvent onButtonClicked;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Pickupable"))
        {
            TriggerEvent();
        }
    }

    private void TriggerEvent()
    {
        onButtonClicked.Invoke();
    }

    public void Debugging()
    {
        Debug.Log("Button Triggered!");
    }
}
