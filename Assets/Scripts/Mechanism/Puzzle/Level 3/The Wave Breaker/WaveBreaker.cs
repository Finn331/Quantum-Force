using UnityEngine;
using cowsins; // karena pakai Crate dari cowsins

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class WaveBreaker : MonoBehaviour
{
    [Header("Cannonball Settings")]
    [SerializeField] private float lifetime = 10f;

    [Header("Impact Tags")]
    [Tooltip("Tag yang akan membuat Cannon Ball hancur (misalnya: Wall, Ground, dll).")]
    [SerializeField] private string[] impactTags = new[] { "GlassDoor" };

    [Header("SFX")]
    [Tooltip("Suara saat bola menabrak.")]
    public AudioClip impactSound;

    private AudioSource audioSource;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.spatialBlend = 1f;
        audioSource.playOnAwake = false;
    }

    private void Start()
    {
        if (lifetime > 0f) Destroy(gameObject, lifetime);
    }

    private void OnCollisionEnter(Collision collision)
    {
        GameObject other = collision.gameObject;

        // 1) Kalau kena GlassBreak langsung hancurkan Crate
        GlassBreak gb = other.GetComponent<GlassBreak>() ??
                        other.GetComponentInParent<GlassBreak>() ??
                        other.GetComponentInChildren<GlassBreak>();

        if (gb != null)
        {
            Crate crate = gb.GetComponent<Crate>() ??
                          gb.GetComponentInParent<Crate>() ??
                          gb.GetComponentInChildren<Crate>();

            if (crate != null)
            {
                crate.Die();
                PlayImpactSFX(collision.GetContact(0).point);
                Destroy(gameObject);
                return;
            }
        }

        // 2) Kalau bukan GlassBreak tapi punya Crate hancurkan juga
        Crate c = other.GetComponent<Crate>() ??
                  other.GetComponentInParent<Crate>() ??
                  other.GetComponentInChildren<Crate>();

        if (c != null)
        {
            c.Die();
            PlayImpactSFX(collision.GetContact(0).point);
            Destroy(gameObject);
            return;
        }

        // 3) Kalau kena tag tertentu (misalnya Wall) bola hancur
        if (IsImpactTag(other.tag))
        {
            PlayImpactSFX(collision.GetContact(0).point);
            Destroy(gameObject);
        }
    }

    private bool IsImpactTag(string t)
    {
        if (impactTags == null) return false;
        foreach (var tag in impactTags)
        {
            if (!string.IsNullOrEmpty(tag) && t == tag) return true;
        }
        return false;
    }

    private void PlayImpactSFX(Vector3 pos)
    {
        if (impactSound != null) AudioSource.PlayClipAtPoint(impactSound, pos);
    }
}
