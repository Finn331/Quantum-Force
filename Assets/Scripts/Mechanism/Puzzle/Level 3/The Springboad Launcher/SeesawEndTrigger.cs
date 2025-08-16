using UnityEngine;

public class SeesawEndTrigger : MonoBehaviour
{
    [Header("Pengaturan Sensor")]
    [Tooltip("Referensi ke skrip SeesawLaunch utama di objek induk.")]
    public SeesawLaunch mainSeesaw;
    [Tooltip("Centang ini jika ujung ini adalah tempat untuk MELETAKKAN BEBAN.")]
    public bool isWeightEnd;
    [Tooltip("Centang ini jika ujung ini adalah tempat PEMAIN BERDIRI.")]
    public bool isPlayerEnd;

    private void OnTriggerEnter(Collider other)
    {
        if (mainSeesaw == null) return;

        // Jika ini adalah ujung untuk pemain dan yang masuk adalah pemain
        if (isPlayerEnd && other.CompareTag("Player"))
        {
            mainSeesaw.OnPlayerEnter(other.GetComponent<Rigidbody>());
        }
        // Jika ini adalah ujung untuk beban
        else if (isWeightEnd)
        {
            mainSeesaw.OnWeightEnter(other);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (mainSeesaw == null) return;

        // Jika ini adalah ujung untuk pemain dan yang keluar adalah pemain
        if (isPlayerEnd && other.CompareTag("Player"))
        {
            mainSeesaw.OnPlayerExit();
        }
    }
}