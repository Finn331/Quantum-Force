using UnityEngine;

public class CatapultTrigger : MonoBehaviour
{
    // Referensi ke skrip Catapult utama
    public Catapult mainCatapult;

    private void Start()
    {
        // Coba cari referensi secara otomatis jika belum di-set
        if (mainCatapult == null)
        {
            mainCatapult = GetComponentInParent<Catapult>();
        }
    }

    // Teruskan event 'OnTriggerEnter' ke skrip utama
    private void OnTriggerEnter(Collider other)
    {
        if (mainCatapult != null)
        {
            mainCatapult.OnObjectEnterTrigger(other);
        }
    }

    // Teruskan event 'OnTriggerExit' ke skrip utama
    private void OnTriggerExit(Collider other)
    {
        if (mainCatapult != null)
        {
            mainCatapult.OnObjectExitTrigger(other);
        }
    }
}