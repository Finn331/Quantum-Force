using UnityEngine;
using UnityEngine.Events; // Diperlukan untuk menggunakan UnityEvent

public class Lever : MonoBehaviour
{
    [Header("Referensi Komponen")]
    [Tooltip("Objek 'handle' dari tuas yang akan berputar.")]
    public Transform handle;

    [Header("Pengaturan Animasi")]
    [Tooltip("Sudut tujuan handle saat ditarik (dalam derajat).")]
    public float pullAngle = 60.0f;
    [Tooltip("Durasi animasi tarikan tuas (dalam detik).")]
    public float animationDuration = 0.5f;

    [Header("Puzzle The Gravity Lever")]
    [SerializeField] GameObject objectHolder;
    [SerializeField] float posY;
    [SerializeField] float timeToMove;

    [Header("Event")]
    [Tooltip("Fungsi yang akan dipicu SETELAH animasi tuas selesai.")]
    public UnityEvent onLeverPulled;

    // Flag untuk memastikan tuas hanya bisa ditarik sekali
    private bool hasBeenPulled = false;

    /// <summary>
    /// Fungsi publik ini dipanggil oleh skrip lain (seperti PlayerInteraction) untuk mengaktifkan tuas.
    /// </summary>
    public void Interact()
    {
        // Jika tuas sudah pernah ditarik, jangan lakukan apa-apa
        if (hasBeenPulled)
        {
            Debug.Log("Tuas ini sudah pernah ditarik.");
            return;
        }

        // Tandai bahwa tuas sudah ditarik
        hasBeenPulled = true;

        Debug.Log("Tuas ditarik!");

        // Pastikan referensi handle ada
        if (handle == null)
        {
            Debug.LogError("Referensi 'Handle' belum di-set di Inspector!", gameObject);
            return;
        }

        // Gunakan LeanTween untuk menganimasikan rotasi handle
        LeanTween.rotateLocal(handle.gameObject, new Vector3(pullAngle, 0, 0), animationDuration)
            .setEase(LeanTweenType.easeOutQuad) // Efek animasi agar lebih halus
            .setOnComplete(TriggerEvent); // Panggil fungsi TriggerEvent setelah animasi selesai
    }

    // Fungsi ini akan dipanggil oleh LeanTween saat animasi selesai
    private void TriggerEvent()
    {
        Debug.Log("Animasi tuas selesai, memicu event...");
        // Picu semua fungsi yang sudah di-set di UnityEvent 'onLeverPulled'
        onLeverPulled.Invoke();
    }

    // Public Function
    public void GravityLever()
    {
        LeanTween.moveLocalY(objectHolder, posY, timeToMove)
            .setEase(LeanTweenType.easeInOutQuad) // Efek animasi agar lebih halus
            .setOnComplete(() => Debug.Log("Objek telah bergerak ke posisi Y: " + posY));
    }
}