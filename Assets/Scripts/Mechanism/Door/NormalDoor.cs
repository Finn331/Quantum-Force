using Unity.VisualScripting;
using UnityEngine;

public class NormalDoor : MonoBehaviour
{
    [Header("Door Settings")]
    [Tooltip("Door Gameobject.")]
    [SerializeField] private GameObject door;
    [Tooltip("Door Position to be moved.")]
    [SerializeField] private float doorPositionY;

    [Header("SFX Settings")]
    [Tooltip("Suara saat pintu terbuka.")]
    [SerializeField] private AudioClip openDoorSFX;
    [Range(0f, 1f)][SerializeField] private float sfxVolume = 1f;

    private AudioSource audioSource;

    void Start()
    {
        // Setup AudioSource otomatis kalau belum ada
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1f; // 3D sound
        }
    }

    public void OpenDoor()
    {
        if (door != null)
        {
            // animasi buka pintu
            LeanTween.moveY(door, doorPositionY, 0.5f)
                     .setEase(LeanTweenType.easeInOutQuad);

            // mainkan SFX
            PlaySFX(openDoorSFX);
        }
        else
        {
            Debug.LogWarning("Door GameObject is not assigned.");
        }
    }

    private void PlaySFX(AudioClip clip)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip, sfxVolume);
        }
    }
}
