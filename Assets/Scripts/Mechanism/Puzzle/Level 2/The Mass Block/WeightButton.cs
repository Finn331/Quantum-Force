using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

[System.Serializable]
public class BoolEvent : UnityEvent<bool> { } // agar bisa on/off di Inspector

public class WeightButton : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Berat (Mass) minimum yang dibutuhkan untuk mengaktifkan tombol ini (kg).")]
    public float requiredMass = 5f;

    [Tooltip("Bagian atas tombol yang bergerak naik/turun.")]
    public Transform buttonTop;

    [Tooltip("Seberapa dalam tombol akan turun saat ditekan (meter).")]
    public float pressDepth = 0.1f;

    [Tooltip("Durasi animasi tombol turun/naik (detik).")]
    public float pressSpeed = 0.3f;

    [Header("Events")]
    [Tooltip("Dipicu saat tombol pertama kali memenuhi massa.")]
    public UnityEvent onPressed;
    [Tooltip("Dipicu saat massa turun di bawah syarat.")]
    public UnityEvent onReleased;
    [Tooltip("Dipicu tiap status berubah: true=pressed, false=released.")]
    public BoolEvent onPressStateChanged;

    [Header("Notify Managers")]
    [Tooltip("Manager yang harus dicek ulang ketika status tombol ini berubah.")]
    public WeightPuzzleManager[] managers;

    private Vector3 topStartPosition;
    private bool isPressed = false;
    private readonly List<Rigidbody> objectsOnButton = new List<Rigidbody>();

    public bool IsPressed => isPressed;

    private void Start()
    {
        if (buttonTop != null)
            topStartPosition = buttonTop.localPosition;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Pickupable")) return;

        var rb = other.attachedRigidbody;
        if (rb != null && !objectsOnButton.Contains(rb))
        {
            objectsOnButton.Add(rb);
            CheckWeight();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Pickupable")) return;

        var rb = other.attachedRigidbody;
        if (rb != null && objectsOnButton.Contains(rb))
        {
            objectsOnButton.Remove(rb);
            CheckWeight();
        }
    }

    private void CheckWeight()
    {
        float totalMass = 0f;
        for (int i = objectsOnButton.Count - 1; i >= 0; i--)
        {
            if (objectsOnButton[i] == null) { objectsOnButton.RemoveAt(i); continue; }
            totalMass += objectsOnButton[i].mass;
        }

        if (totalMass >= requiredMass && !isPressed)
        {
            PressButton();
        }
        else if (totalMass < requiredMass && isPressed)
        {
            ReleaseButton();
        }
        // jika status tidak berubah, tidak ada notifikasi
    }

    private void PressButton()
    {
        isPressed = true;
        if (buttonTop != null)
            LeanTween.moveLocalY(buttonTop.gameObject, topStartPosition.y - pressDepth, pressSpeed).setEaseOutQuad();

        onPressed?.Invoke();
        onPressStateChanged?.Invoke(true);
        NotifyManagers();
    }

    private void ReleaseButton()
    {
        isPressed = false;
        if (buttonTop != null)
            LeanTween.moveLocalY(buttonTop.gameObject, topStartPosition.y, pressSpeed).setEaseInQuad();

        onReleased?.Invoke();
        onPressStateChanged?.Invoke(false);
        NotifyManagers();
    }

    private void NotifyManagers()
    {
        if (managers == null) return;
        foreach (var m in managers)
            if (m != null) m.CheckPuzzleState();
    }
}
