using UnityEngine;
using TMPro;
using System.Collections;

[RequireComponent(typeof(CanvasGroup))]
public class SubtitleUI : MonoBehaviour
{
    public static SubtitleUI Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI subtitleText;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private GameObject subtitlePanel;

    [Header("Animation Settings")]
    [SerializeField] private float fadeInDuration = 0.2f;
    [SerializeField] private float fadeOutDuration = 0.3f;

    private Coroutine currentCoroutine;
    private bool isVisible = false;

    private void Awake()
    {
        // Singleton (allow multiple instances in different scenes, keep the latest)
        if (Instance != null && Instance != this)
        {
            Destroy(Instance.gameObject);
        }
        Instance = this;

        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        // Start hidden
        HideImmediate();
    }

    private void OnEnable()
    {
        // Listen to language changes to update displayed text if needed
        LanguageManager.OnLanguageChanged += OnLanguageChanged;
        SubtitleManager.OnSubtitleSettingChanged += OnSubtitleSettingChanged;
    }

    private void OnDisable()
    {
        LanguageManager.OnLanguageChanged -= OnLanguageChanged;
        SubtitleManager.OnSubtitleSettingChanged -= OnSubtitleSettingChanged;
    }

    private void OnLanguageChanged(GameLanguage newLanguage)
    {
        // Language changed, text will be updated on next ShowSubtitle call
    }

    private void OnSubtitleSettingChanged(bool enabled)
    {
        if (!enabled)
        {
            HideSubtitle();
        }
    }

    /// <summary>
    /// Show subtitle with the given text
    /// </summary>
    public void ShowSubtitle(string text)
    {
        if (!SubtitleManager.SubtitleEnabled)
            return;

        if (string.IsNullOrEmpty(text))
        {
            HideSubtitle();
            return;
        }

        if (currentCoroutine != null)
            StopCoroutine(currentCoroutine);

        currentCoroutine = StartCoroutine(ShowSubtitleCoroutine(text));
    }

    /// <summary>
    /// Show subtitle with auto-hide after duration
    /// </summary>
    public void ShowSubtitle(string text, float duration)
    {
        if (!SubtitleManager.SubtitleEnabled)
            return;

        if (string.IsNullOrEmpty(text))
        {
            HideSubtitle();
            return;
        }

        if (currentCoroutine != null)
            StopCoroutine(currentCoroutine);

        currentCoroutine = StartCoroutine(ShowSubtitleWithDurationCoroutine(text, duration));
    }

    /// <summary>
    /// Hide subtitle with fade animation
    /// </summary>
    public void HideSubtitle()
    {
        if (currentCoroutine != null)
            StopCoroutine(currentCoroutine);

        if (isVisible)
        {
            currentCoroutine = StartCoroutine(HideSubtitleCoroutine());
        }
    }

    /// <summary>
    /// Hide subtitle immediately without animation
    /// </summary>
    public void HideImmediate()
    {
        if (currentCoroutine != null)
            StopCoroutine(currentCoroutine);

        canvasGroup.alpha = 0f;
        if (subtitlePanel != null)
            subtitlePanel.SetActive(false);
        isVisible = false;
    }

    private IEnumerator ShowSubtitleCoroutine(string text)
    {
        subtitleText.text = text;

        if (subtitlePanel != null)
            subtitlePanel.SetActive(true);

        // Fade in
        float elapsed = 0f;
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / fadeInDuration);
            yield return null;
        }

        canvasGroup.alpha = 1f;
        isVisible = true;
    }

    private IEnumerator ShowSubtitleWithDurationCoroutine(string text, float duration)
    {
        yield return ShowSubtitleCoroutine(text);

        // Wait for duration
        yield return new WaitForSeconds(duration);

        // Fade out
        yield return HideSubtitleCoroutine();
    }

    private IEnumerator HideSubtitleCoroutine()
    {
        float elapsed = 0f;
        float startAlpha = canvasGroup.alpha;

        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, elapsed / fadeOutDuration);
            yield return null;
        }

        canvasGroup.alpha = 0f;
        if (subtitlePanel != null)
            subtitlePanel.SetActive(false);
        isVisible = false;
    }
}
