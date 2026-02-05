using UnityEngine;
using UnityEngine.Events;

public class ButtonTrigger : MonoBehaviour
{
    [Header("Ball Requirements")]
    [Tooltip("Minimal jumlah pantulan (currBounce) yang dibutuhkan agar tombol memicu event.")]
    [SerializeField] private int requiredBounce = 1;

    [Tooltip("Jika true: syaratnya currBounce >= requiredBounce. Jika false: harus tepat sama (==).")]
    [SerializeField] private bool allowGreaterOrEqual = true;

    [Tooltip("Reset currBounce milik Ball ke 0 setelah tombol terpicu.")]
    [SerializeField] private bool resetBounceOnTrigger = false;

    [Tooltip("Bolehkan tombol dipicu beberapa kali oleh objek yang sama?")]
    [SerializeField] private bool allowMultipleTriggers = true;

    [Header("Emissive Settings")]
    [SerializeField] GameObject deactivateButton;
    [SerializeField] GameObject activateButton;

    [Header("Event")]
    [Tooltip("Fungsi yang akan dipicu SETELAH syarat terpenuhi.")]
    public UnityEvent onButtonClicked;

    // Simpan referensi Ball terakhir yang sudah memicu (untuk mencegah double-trigger jika diinginkan)
    private System.Collections.Generic.HashSet<Ball> _hasTriggeredFromBall = new System.Collections.Generic.HashSet<Ball>();

    private void OnCollisionEnter(Collision collision)
    {
        // Pastikan ini objek yang boleh menekan (sesuai logikamu sebelumnya)
        if (!collision.gameObject.CompareTag("Pickupable")) return;

        // Cari komponen Ball pada objek yang bertabrakan (langsung atau di parent)
        Ball ball = collision.gameObject.GetComponent<Ball>();
        if (ball == null)
        {
            ball = collision.gameObject.GetComponentInParent<Ball>();
        }
        if (ball == null)
        {
            // Bukan bola? Abaikan.
            return;
        }

        // Cegah trigger berulang dari bola yang sama (kecuali diizinkan)
        if (!allowMultipleTriggers && _hasTriggeredFromBall.Contains(ball))
            return;

        // Cek syarat currBounce
        bool pass = allowGreaterOrEqual ? (ball.currBounce >= requiredBounce)
                                        : (ball.currBounce == requiredBounce);

        if (pass)
        {
            TriggerEvent();

            // Tandai sudah memicu
            _hasTriggeredFromBall.Add(ball);

            if (resetBounceOnTrigger)
                ball.currBounce = 0;
        }
        else
        {
            Debug.Log($"[ButtonTrigger] Syarat belum terpenuhi. currBounce={ball.currBounce}, butuh {(allowGreaterOrEqual ? ">=" : "==")} {requiredBounce}", this);
        }
    }

    private void TriggerEvent()
    {
        onButtonClicked?.Invoke();
    }

    // Utility buat debug manual dari Inspector
    public void Activated()
    {
        deactivateButton.SetActive(false);
        activateButton.SetActive(true);
    }
}
