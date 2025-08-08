using UnityEngine;

public class CatapultHandle : MonoBehaviour
{
    // Referensi ke skrip Catapult utama
    public Catapult mainCatapult;

    public void Interact()
    {
        if (mainCatapult != null)
        {
            if (mainCatapult.IsReadyToLaunch())
            {
                mainCatapult.Launch();
                Debug.Log("Catapult diaktifkan via Raycast!");
            }
            else
            {
                Debug.Log("Catapult tidak siap! Tidak ada objek untuk dilontarkan.");
            }
        }
        else
        {
            Debug.LogError("Referensi Main Catapult belum di-set di Handle!", gameObject);
        }
    }
}