using UnityEngine;
using UnityEngine.Events;

public class WeightButton : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Berat (Mass) yang dibutuhkan untuk mengaktifkan tombol ini (dalam kg).")]
    public float requiredMass;
    [Tooltip("Referensi ke objek visual bagian atas tombol yang akan bergerak.")]
    public Transform buttonTop;
    [Tooltip("Seberapa dalam tombol akan turun saat ditekan (dalam meter).")]
    public float pressDepth = 0.1f;
    [Tooltip("Kecepatan animasi tombol turun/naik.")]
    public float pressSpeed = 0.3f;

    [Header("Events")]
    [Tooltip("Event yang dipicu saat tombol ditekan dengan benar.")]
    public UnityEvent onPressed;
    [Tooltip("Event yang dipicu saat objek diangkat dari tombol.")]
    public UnityEvent onReleased;

    private Vector3 topStartPosition;
    private bool isPressed = false;

    // Properti agar Manager bisa tahu status tombol ini
    public bool IsPressed => isPressed;

    private void Start()
    {
        if (buttonTop != null)
        {
            // Simpan posisi awal dari bagian atas tombol
            topStartPosition = buttonTop.localPosition;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Cek jika objek yang masuk punya tag "Pickupable" dan belum ditekan
        if (other.CompareTag("Pickupable") && !isPressed)
        {
            // Coba dapatkan komponen Rigidbody dari objek
            Rigidbody rb = other.GetComponent<Rigidbody>();
            if (rb != null)
            {
                // Cek apakah mass-nya sesuai (gunakan Mathf.Approximately untuk perbandingan float)
                if (Mathf.Approximately(rb.mass, requiredMass))
                {
                    PressButton();
                }
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // Cek jika objek yang keluar punya tag "Pickupable" dan tombol sedang ditekan
        if (other.CompareTag("Pickupable") && isPressed)
        {
            Rigidbody rb = other.GetComponent<Rigidbody>();
            if (rb != null && Mathf.Approximately(rb.mass, requiredMass))
            {
                ReleaseButton();
            }
        }
    }

    private void PressButton()
    {
        isPressed = true;
        Debug.Log("Tombol untuk " + requiredMass + "kg DITEKAN.");

        // Animasikan tombol turun menggunakan LeanTween
        if (buttonTop != null)
        {
            LeanTween.moveLocalY(buttonTop.gameObject, topStartPosition.y - pressDepth, pressSpeed)
                .setEaseOutQuad();
        }

        // Picu event 'onPressed'
        onPressed.Invoke();
    }

    private void ReleaseButton()
    {
        isPressed = false;
        Debug.Log("Tombol untuk " + requiredMass + "kg DILEPAS.");

        // Animasikan tombol kembali ke atas
        if (buttonTop != null)
        {
            LeanTween.moveLocalY(buttonTop.gameObject, topStartPosition.y, pressSpeed)
                .setEaseInQuad();
        }

        // Picu event 'onReleased'
        onReleased.Invoke();
    }
}