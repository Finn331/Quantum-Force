using UnityEngine;

public class VoiceOver : MonoBehaviour
{
    [Header("Audio Settings")]
    [SerializeField] private AudioClip voiceOverSFX;
    [SerializeField] private float sfxVolume = 1f;

    private AudioSource audioSource;

    private void Awake()
    {
        // Tambahkan AudioSource otomatis jika belum ada
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f; // 3D sound, bisa diubah ke 0f untuk 2D
        }
        else
        {
            Debug.LogWarning("VoiceOver: GameObject sudah memiliki AudioSource. Pastikan pengaturan AudioSource sesuai kebutuhan.");
        }
    }

    public void PlayAudio()
    {
        if (voiceOverSFX != null)
        {
            audioSource.clip = voiceOverSFX;
            audioSource.volume = sfxVolume;
            audioSource.Play();
        }
        else
        {
            Debug.LogWarning("VoiceOver: Tidak ada AudioClip yang di-assign!");
        }
    }
}
