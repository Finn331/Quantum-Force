using UnityEngine;
using System.Collections;

[RequireComponent(typeof(AudioSource))]
public class VoiceOver : MonoBehaviour
{
    [Header("Audio Settings")]
    [SerializeField] private AudioClip indonesianClip;
    [SerializeField] private AudioClip englishClip;
    [SerializeField, Range(0f, 1f)] private float sfxVolume = 1f;

    [Header("Subtitle Settings")]
    [SerializeField] private SubtitleData subtitleData;
    [Tooltip("Optional: Will find SubtitleUI.Instance in scene if not assigned")]
    [SerializeField] private SubtitleUI subtitleUI;

    private AudioSource audioSource;
    private Coroutine subtitleCoroutine;

    private void Awake()
    {
        // Tambahkan / ambil AudioSource
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f; // 0 = 2D sound
    }

    private AudioClip GetClipByLanguage()
    {
        // Kalau tidak ada LanguageManager (misal di scene test), fallback ke Indonesian
        GameLanguage lang = GameLanguage.Indonesian;
        if (LanguageManager.Instance != null)
        {
            lang = LanguageManager.CurrentLanguage;
        }

        switch (lang)
        {
            case GameLanguage.Indonesian:
                return indonesianClip != null ? indonesianClip : englishClip; // fallback
            case GameLanguage.English:
                return englishClip != null ? englishClip : indonesianClip; // fallback
            default:
                return indonesianClip;
        }
    }

    public void PlayAudio()
    {
        AudioClip clip = GetClipByLanguage();

        if (clip != null)
        {
            audioSource.clip = clip;
            audioSource.volume = sfxVolume;
            audioSource.Play();
            
            // Play subtitles if enabled
            PlaySubtitles();
        }
        else
        {
            Debug.LogWarning($"VoiceOver ({name}): Tidak ada AudioClip untuk bahasa ini!");
        }
    }

    // Optional: kalau mau dipanggil dari code lain dengan override bahasa
    public void PlayAudioWithLanguage(GameLanguage language)
    {
        GameLanguage old = LanguageManager.CurrentLanguage;
        AudioClip clip = (language == GameLanguage.Indonesian) ? indonesianClip : englishClip;

        if (clip != null)
        {
            audioSource.clip = clip;
            audioSource.volume = sfxVolume;
            audioSource.Play();
            
            // Play subtitles if enabled
            PlaySubtitles();
        }
        else
        {
            Debug.LogWarning($"VoiceOver ({name}): Clip untuk {language} belum di-assign!");
        }
    }

    /// <summary>
    /// Stop audio and hide subtitles
    /// </summary>
    public void StopAudio()
    {
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }
        
        StopSubtitles();
    }

    private void PlaySubtitles()
    {
        // Check if subtitles are enabled
        if (SubtitleManager.Instance == null || !SubtitleManager.SubtitleEnabled)
            return;

        // Check if we have subtitle data
        if (subtitleData == null || subtitleData.entries.Count == 0)
            return;

        // Get SubtitleUI instance
        SubtitleUI ui = subtitleUI != null ? subtitleUI : SubtitleUI.Instance;
        if (ui == null)
            return;

        // Stop any existing subtitle coroutine
        if (subtitleCoroutine != null)
            StopCoroutine(subtitleCoroutine);

        subtitleCoroutine = StartCoroutine(PlaySubtitleSequence(ui));
    }

    private void StopSubtitles()
    {
        if (subtitleCoroutine != null)
        {
            StopCoroutine(subtitleCoroutine);
            subtitleCoroutine = null;
        }

        // Hide subtitle UI
        SubtitleUI ui = subtitleUI != null ? subtitleUI : SubtitleUI.Instance;
        if (ui != null)
        {
            ui.HideSubtitle();
        }
    }

    private IEnumerator PlaySubtitleSequence(SubtitleUI ui)
    {
        var sortedEntries = subtitleData.GetSortedEntries();
        float startTime = Time.time;

        foreach (var entry in sortedEntries)
        {
            // Wait until entry start time
            float waitTime = entry.startTime - (Time.time - startTime);
            if (waitTime > 0)
            {
                yield return new WaitForSeconds(waitTime);
            }

            // Check if audio is still playing
            if (audioSource == null || !audioSource.isPlaying)
            {
                ui.HideSubtitle();
                yield break;
            }

            // Check if subtitles are still enabled
            if (!SubtitleManager.SubtitleEnabled)
            {
                ui.HideSubtitle();
                yield break;
            }

            // Show subtitle
            string text = entry.GetText();
            ui.ShowSubtitle(text, entry.duration);

            // Wait for duration
            yield return new WaitForSeconds(entry.duration);
        }

        // Hide subtitle after all entries complete
        ui.HideSubtitle();
        subtitleCoroutine = null;
    }

    private void OnDisable()
    {
        StopSubtitles();
    }
}

