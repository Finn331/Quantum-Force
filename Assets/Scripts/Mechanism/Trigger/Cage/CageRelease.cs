using UnityEngine;

public class CageRelease : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] GameObject cage;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            ReleaseCage();
        }
    }

    public void ReleaseCage()
    {
        if (cage != null)
        {
            cage.SetActive(false); // Deactivate the cage GameObject            
            Destroy(cage, 1f); // Optional: delay before destroying the cage
            Debug.Log("Cage released!");
            Destroy(gameObject); // Destroy this script's GameObject
        }
        else
        {
            Debug.LogWarning("Cage GameObject is not assigned.");
        }
    }
}
