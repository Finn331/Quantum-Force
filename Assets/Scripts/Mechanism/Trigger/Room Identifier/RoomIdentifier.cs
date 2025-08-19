using TMPro;
using UnityEngine;

public class RoomIdentifier : MonoBehaviour
{
    [Header("Text Setting")]
    [SerializeField] string roomName;
    [SerializeField] TextMeshProUGUI roomNameTextUI;
    [SerializeField] string descriptionText;
    [SerializeField] TextMeshProUGUI descriptionTextUI;

    [Header("GameObject Setting")]
    [SerializeField] GameObject panelTextIdentifier;

    [Header("Animation Settings")]
    [Tooltip("Waktu jeda agar pemain bisa membaca teks (dalam detik).")]
    [SerializeField] float readTime = 2.5f;

    // Variabel baru untuk menyimpan CanvasGroup
    private CanvasGroup descriptionCanvasGroup;

    [SerializeField] GameObject triggerGameobject;

    private void Awake()
    {
        // --- PERBAIKAN UTAMA ---
        // Siapkan CanvasGroup pada descriptionTextUI.
        // Ini adalah cara terbaik untuk mengontrol alpha UI.
        if (descriptionTextUI != null)
        {
            descriptionCanvasGroup = descriptionTextUI.GetComponent<CanvasGroup>();
            if (descriptionCanvasGroup == null)
            {
                descriptionCanvasGroup = descriptionTextUI.gameObject.AddComponent<CanvasGroup>();
            }
        }
    }

    void Start()
    {
        if (panelTextIdentifier != null) panelTextIdentifier.SetActive(false);
        if (roomNameTextUI != null) roomNameTextUI.text = roomName;
        if (descriptionTextUI != null) descriptionTextUI.text = descriptionText;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            ShowText();
            Destroy(triggerGameobject); // Hapus trigger setelah digunakan
        }
    }

    public void ShowText()
    {        
        if (roomNameTextUI != null) LeanTween.cancel(roomNameTextUI.gameObject);
        if (descriptionCanvasGroup != null) LeanTween.cancel(descriptionCanvasGroup.gameObject);

        // 1. Persiapan Awal
        if (panelTextIdentifier != null) panelTextIdentifier.SetActive(true);
        if (roomNameTextUI != null) roomNameTextUI.gameObject.SetActive(true);
        if (descriptionTextUI != null) descriptionTextUI.gameObject.SetActive(true);

        roomNameTextUI.transform.localScale = Vector3.zero;

        // Atur alpha awal melalui CanvasGroup
        if (descriptionCanvasGroup != null) descriptionCanvasGroup.alpha = 0f;

        // 2. Buat Urutan Animasi (Sequence)
        LeanTween.sequence()
            .append(LeanTween.scale(roomNameTextUI.gameObject, Vector3.one, 0.5f).setEase(LeanTweenType.easeOutBack))

            // --- PERBAIKAN DI SINI ---
            // Gunakan LeanTween.alphaCanvas() untuk fade in
            .append(LeanTween.alphaCanvas(descriptionCanvasGroup, 1f, 0.5f).setEase(LeanTweenType.easeInOutSine))

            .append(readTime)

            .append(() => {
                LeanTween.scale(roomNameTextUI.gameObject, Vector3.zero, 0.5f).setEase(LeanTweenType.easeInBack);
                // Gunakan LeanTween.alphaCanvas() untuk fade out
                LeanTween.alphaCanvas(descriptionCanvasGroup, 0f, 0.5f).setEase(LeanTweenType.easeInOutSine);
            })

            .append(0.5f)

            .append(() => {
                if (panelTextIdentifier != null) panelTextIdentifier.SetActive(false);
            });
    }
}