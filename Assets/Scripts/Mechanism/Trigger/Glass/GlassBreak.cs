using cowsins;
using UnityEngine;

public class GlassBreak : MonoBehaviour
{
    //Script Reference
    private Crate crate;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        crate = GetComponent<Crate>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    private void OnTriggerEnter(Collider other)
    {
        // Cek apakah objek yang masuk memiliki tag "Player"
        if (other.CompareTag("Player"))
        {            
            crate.Die();
        }
    }
}

