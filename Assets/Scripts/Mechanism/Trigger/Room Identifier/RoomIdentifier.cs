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

    private CanvasGroup descriptionCanvasGroup;
    [SerializeField] GameObject triggerGameobject;

    private void Awake()
    {
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
        // We no longer set the text here.
        // We only ensure the panel is hidden at the start.
        if (panelTextIdentifier != null) panelTextIdentifier.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            ShowText();
            if (triggerGameobject != null)
            {
                Destroy(triggerGameobject); // Destroy the trigger after use
            }
            else
            {
                Destroy(gameObject); // Fallback to destroying this object
            }
        }
    }

    public void ShowText()
    {
        // --- PERBAIKAN UTAMA DI SINI ---
        // 1. Set the text content RIGHT NOW, using this trigger's specific text.
        if (roomNameTextUI != null) roomNameTextUI.text = roomName;
        if (descriptionTextUI != null) descriptionTextUI.text = descriptionText;

        // Stop any previous animations to prevent overlap
        if (roomNameTextUI != null) LeanTween.cancel(roomNameTextUI.gameObject);
        if (descriptionCanvasGroup != null) LeanTween.cancel(descriptionCanvasGroup.gameObject);

        // 2. Prepare the UI for animation
        if (panelTextIdentifier != null) panelTextIdentifier.SetActive(true);
        if (roomNameTextUI != null) roomNameTextUI.gameObject.SetActive(true);
        if (descriptionTextUI != null) descriptionTextUI.gameObject.SetActive(true);

        roomNameTextUI.transform.localScale = Vector3.zero;
        if (descriptionCanvasGroup != null) descriptionCanvasGroup.alpha = 0f;

        // 3. Create the animation sequence
        LeanTween.sequence()
            .append(LeanTween.scale(roomNameTextUI.gameObject, Vector3.one, 0.5f).setEase(LeanTweenType.easeOutBack))
            .append(LeanTween.alphaCanvas(descriptionCanvasGroup, 1f, 0.5f).setEase(LeanTweenType.easeInOutSine))
            .append(readTime)
            .append(() => {
                LeanTween.scale(roomNameTextUI.gameObject, Vector3.zero, 0.5f).setEase(LeanTweenType.easeInBack);
                LeanTween.alphaCanvas(descriptionCanvasGroup, 0f, 0.5f).setEase(LeanTweenType.easeInOutSine);
            })
            .append(0.5f)
            .append(() => {
                if (panelTextIdentifier != null) panelTextIdentifier.SetActive(false);
            });
    }
}