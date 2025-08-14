using UnityEngine;
using UnityEngine.Events; // Diperlukan untuk menggunakan UnityEvent

public class Lever : MonoBehaviour
{
    [Header("Referensi Komponen")]
    [Tooltip("Objek 'handle' dari tuas yang akan berputar.")]
    public Transform handle;

    [Header("Pengaturan Tuas")]
    [Tooltip("Centang ini jika tuas bisa ditarik berkali-kali (on/off). Jika tidak, tuas hanya bisa digunakan sekali.")]
    public bool isRepeatable = false;

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
    [Tooltip("Fungsi yang akan dipicu saat tuas DITARIK TURUN.")]
    public UnityEvent onLeverPulled;
    [Tooltip("Fungsi yang akan dipicu saat tuas DIKEMBALIKAN (hanya jika isRepeatable true).")]
    public UnityEvent onLeverReturned;

    // Variabel internal untuk melacak status tuas
    private bool isPulled = false;
    private bool hasBeenPulledOnce = false;
    private Vector3 initialRotation;

    private void Start()
    {
        if (handle != null)
        {
            initialRotation = handle.localEulerAngles;
        }
    }

    /// <summary>
    /// Fungsi publik ini dipanggil oleh skrip lain (seperti PlayerInteraction) untuk mengaktifkan tuas.
    /// </summary>
    public void Interact()
    {
        // Cegah interaksi jika animasi sedang berjalan
        if (handle != null && LeanTween.isTweening(handle.gameObject))
        {
            return;
        }

        // --- LOGIKA UTAMA ---
        if (isRepeatable)
        {
            // --- LOGIKA UNTUK TUAS YANG BISA DIULANG ---
            isPulled = !isPulled; // Balik status on/off
            AnimateLever(isPulled);
        }
        else
        {
            // --- LOGIKA UNTUK TUAS SEKALI PAKAI ---
            if (hasBeenPulledOnce)
            {
                Debug.Log("Tuas ini sudah pernah ditarik.");
                return;
            }
            hasBeenPulledOnce = true;
            AnimateLever(true); // Tarik tuas ke posisi on
        }
    }

    private void AnimateLever(bool pullDown)
    {
        if (handle == null)
        {
            Debug.LogError("Referensi 'Handle' belum di-set di Inspector!", gameObject);
            return;
        }

        if (pullDown)
        {
            Debug.Log("Tuas ditarik!");
            // Animasikan handle ke posisi 'pullAngle'
            LeanTween.rotateLocal(handle.gameObject, new Vector3(pullAngle, initialRotation.y, initialRotation.z), animationDuration)
                .setEase(LeanTweenType.easeOutQuad)
                .setOnComplete(() => {
                    Debug.Log("Animasi tuas turun selesai, memicu event OnLeverPulled...");
                    onLeverPulled.Invoke();
                });
        }
        else
        {
            Debug.Log("Tuas dikembalikan!");
            // Animasikan handle kembali ke rotasi awal
            LeanTween.rotateLocal(handle.gameObject, initialRotation, animationDuration)
                .setEase(LeanTweenType.easeInQuad)
                .setOnComplete(() => {
                    Debug.Log("Animasi tuas naik selesai, memicu event OnLeverReturned...");
                    onLeverReturned.Invoke();
                });
        }
    }

    // Fungsi ini tetap ada jika Anda membutuhkannya untuk puzzle spesifik
    public void GravityLever()
    {
        LeanTween.moveLocalY(objectHolder, posY, timeToMove)
            .setEase(LeanTweenType.easeInOutQuad)
            .setOnComplete(() => Debug.Log("Objek telah bergerak ke posisi Y: " + posY));
    }
}