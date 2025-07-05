using UnityEngine;
using System.Collections;

namespace cowsins
{
    public class SoundManager : MonoBehaviour
    {
        public static SoundManager Instance;

        private AudioSource src;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                transform.SetParent(null);
            }
            else Destroy(this.gameObject);

            src = GetComponent<AudioSource>();
        }

        public void PlaySound(AudioClip clip, float delay, float pitchAdded, bool randomPitch, float spatialBlend, float volume = 1f)
        {
            StartCoroutine(Play(clip, delay, pitchAdded, randomPitch, spatialBlend, volume));
        }

        private IEnumerator Play(AudioClip clip, float delay, float pitch, bool randomPitch, float spatialBlend, float volume)
        {
            if (clip == null) yield break;

            yield return new WaitForSeconds(delay);

            src.spatialBlend = spatialBlend;
            float pitchAdded = randomPitch ? Random.Range(-pitch, pitch) : pitch;
            src.pitch = 1 + pitchAdded;

            src.PlayOneShot(clip, volume);
            yield return null;
        }
    }
}
