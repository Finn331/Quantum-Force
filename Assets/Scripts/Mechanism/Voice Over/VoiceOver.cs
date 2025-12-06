using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class VoiceOver : MonoBehaviour
{
    [Header("Audio Settings")]
    [SerializeField] private AudioClip indonesianClip;
    [SerializeField] private AudioClip englishClip;
    [SerializeField, Range(0f, 1f)] private float sfxVolume = 1f;

    private AudioSource audioSource;

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
        }
        else
        {
            Debug.LogWarning($"VoiceOver ({name}): Clip untuk {language} belum di-assign!");
        }
    }
}
