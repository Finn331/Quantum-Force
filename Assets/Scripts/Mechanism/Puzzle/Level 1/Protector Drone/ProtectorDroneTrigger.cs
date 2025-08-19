using UnityEngine;

public class ProtectorDroneTrigger : MonoBehaviour
{
    private ProtectorDrone protectorDrone;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        protectorDrone = GetComponentInParent<ProtectorDrone>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void EnableScript()
    {
        if (protectorDrone != null)
        {
            protectorDrone.enabled = true;
        }
        else
        {
            Debug.LogError("ProtectorDrone tidak ditemukan pada parent!", gameObject);
        }
    }
}
