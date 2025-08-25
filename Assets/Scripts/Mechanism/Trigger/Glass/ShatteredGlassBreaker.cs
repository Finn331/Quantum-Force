using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class ShatteredGlassBreaker : MonoBehaviour
{
    [Header("Pieces Source")]
    [Tooltip("Jika kosong, semua child langsung di bawah GameObject ini akan dianggap sebagai pecahan.")]
    [SerializeField] private Transform piecesParent;
    [Tooltip("Opsional: hanya ambil child yang bertag ini. Kosongkan untuk ambil semua.")]
    [SerializeField] private string filterTag = "";

    [Header("Timing (Stagger)")]
    [Tooltip("Waktu dasar sebelum tiap pecahan dihancurkan (detik).")]
    [SerializeField] private float baseDelay = 1.0f;
    [Tooltip("Jitter acak per pecahan (0..jitter) ditambahkan di atas baseDelay.")]
    [SerializeField] private float randomJitter = 0.3f;
    [Tooltip("Urutan acak? Kalau tidak, urutan sesuai hierarchy.")]
    [SerializeField] private bool randomizeOrder = true;

    [Header("Per-Piece Effect")]
    [Tooltip("Durasi transisi visual per pecahan (fade/scale), detik.")]
    [SerializeField] private float pieceTweenDuration = 0.25f;
    [Tooltip("Gunakan LeanTween untuk fade/scale sebelum destroy (butuh shader yang support Color/alpha).")]
    [SerializeField] private bool useLeanTween = true;
    [Tooltip("Kecilkan skala ke 0 sebelum destroy.")]
    [SerializeField] private bool scaleDown = true;
    [Tooltip("Fade material color.a ke 0 sebelum destroy (butuh material instanced & shader pakai _Color).")]
    [SerializeField] private bool fadeOut = false;

    [Header("Physics Option")]
    [Tooltip("Aktifkan fisika & beri dorongan/explosion per pecahan sebelum dihancurkan.")]
    [SerializeField] private bool enablePhysics = true;
    [Tooltip("Kekuatan dorongan awal lurus ke depan (di local forward pecahan).")]
    [SerializeField] private float pushForce = 0.5f;
    [Tooltip("Kekuatan 'ledakan' kecil yang menyebar dari titik ini.")]
    [SerializeField] private float explosionForce = 15f;
    [SerializeField] private float explosionRadius = 1.5f;
    [SerializeField] private Vector3 extraTorque = new Vector3(0, 45f, 0);

    [Header("Audio (Optional)")]
    [SerializeField] private AudioClip shatterSfx;
    [SerializeField] private float sfxVolume = 0.8f;

    [Header("Run")]
    [Tooltip("Mulai pecahkan otomatis saat Start.")]
    [SerializeField] private bool breakOnStart = false;

    [Header("Events")]
    public UnityEvent onBreakStarted;
    public UnityEvent onAllPiecesDestroyed;

    private readonly List<GameObject> _pieces = new List<GameObject>();
    private bool _running;

    private void Reset()
    {
        piecesParent = transform;
    }

    private void Start()
    {
        if (piecesParent == null) piecesParent = transform;
        CollectPieces();

        if (breakOnStart)
            BreakNow();
    }

    /// <summary> Kumpulkan pecahan dari child. </summary>
    public void CollectPieces()
    {
        _pieces.Clear();
        if (piecesParent == null) piecesParent = transform;

        for (int i = 0; i < piecesParent.childCount; i++)
        {
            var c = piecesParent.GetChild(i);
            if (!string.IsNullOrEmpty(filterTag) && c.tag != filterTag) continue;
            _pieces.Add(c.gameObject);
        }
    }

    /// <summary> Mulai proses penghancuran bertahap. </summary>
    public void BreakNow()
    {
        if (_running) return;
        StartCoroutine(BreakRoutine());
    }

    private IEnumerator BreakRoutine()
    {
        _running = true;
        onBreakStarted?.Invoke();

        if (randomizeOrder)
            Shuffle(_pieces);

        int remaining = 0;
        foreach (var go in _pieces)
            if (go != null && go.activeInHierarchy) remaining++;

        // main SFX sekali di awal (opsional)
        if (shatterSfx != null && remaining > 0)
            AudioSource.PlayClipAtPoint(shatterSfx, transform.position, sfxVolume);

        float now = Time.time;

        for (int i = 0; i < _pieces.Count; i++)
        {
            var piece = _pieces[i];
            if (piece == null) continue;

            float delay = baseDelay + Random.Range(0f, Mathf.Max(0f, randomJitter));
            // jadwalkan tiap pecahan tanpa menunggu yang lain (serentak tapi beda timing)
            StartCoroutine(DestroyPieceWithEffects(piece, delay));
        }

        // tunggu semua selesai (aproksimasi)
        float maxDelay = baseDelay + Mathf.Max(0f, randomJitter) + pieceTweenDuration + 0.1f;
        yield return new WaitForSeconds(maxDelay);

        onAllPiecesDestroyed?.Invoke();
        _running = false;
    }

    private IEnumerator DestroyPieceWithEffects(GameObject piece, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (piece == null) yield break;

        // Physics/dorongan
        if (enablePhysics)
        {
            var col = piece.GetComponent<Collider>();
            if (col != null) col.enabled = true;

            var rb = piece.GetComponent<Rigidbody>();
            if (rb == null) rb = piece.AddComponent<Rigidbody>();

#if UNITY_6000_0_OR_NEWER
            rb.linearVelocity = Vector3.zero;
#else
            rb.velocity = Vector3.zero;
#endif
            rb.angularVelocity = Vector3.zero;

            // dorongan kecil dan explosion
            Vector3 fwd = piece.transform.forward;
#if UNITY_6000_0_OR_NEWER
            rb.linearVelocity = fwd * pushForce;
#else
            rb.velocity = fwd * pushForce;
#endif
            rb.AddExplosionForce(explosionForce, piece.transform.position, explosionRadius, 0.0f, ForceMode.Impulse);
            rb.AddTorque(extraTorque, ForceMode.Impulse);
        }

        // Tween visual (opsional, butuh LeanTween)
        if (useLeanTween)
        {
            if (scaleDown)
            {
                LeanTween.scale(piece, Vector3.zero, pieceTweenDuration)
                         .setEaseInBack();
            }
            if (fadeOut)
            {
                // coba ambil renderer & ubah warna (instancing material!)
                var rend = piece.GetComponent<Renderer>();
                if (rend != null && rend.material.HasProperty("_Color"))
                {
                    var m = rend.material; // instanced
                    Color c = m.color;
                    LeanTween.value(piece, c.a, 0f, pieceTweenDuration)
                             .setOnUpdate((float a) =>
                             {
                                 var cc = m.color; cc.a = a; m.color = cc;
                             });
                }
            }
            yield return new WaitForSeconds(pieceTweenDuration);
        }

        if (piece != null)
            Destroy(piece);
    }

    private static void Shuffle<T>(IList<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int j = Random.Range(i, list.Count);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}
