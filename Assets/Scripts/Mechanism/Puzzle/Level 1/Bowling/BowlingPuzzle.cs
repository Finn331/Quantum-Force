using cowsins;
using UnityEngine;
using UnityEngine.Events;

public class BowlingPuzzle : MonoBehaviour
{
    [Header("Bowling Setting")]
    [SerializeField] private Transform goalPos;
    [SerializeField] private bool isGoalSet = false;

    [Header("Goals Setting")]
    [SerializeField] private GameObject bridge;
    [SerializeField] private float moveYPos = 0f;
    [SerializeField] private AudioClip triggerSFX;

    [Header("Script Reference")]
    [SerializeField] private Crate crate;

    [Header("Custom Events")]
    [Tooltip("Dipanggil sekali saat goal tercapai (bola valid masuk ke trigger).")]
    [SerializeField] private UnityEvent onGoalSet;

    // Script Reference
    private BowlingPuzzle bowlingPuzzle;

    void Start()
    {
        bowlingPuzzle = this.GetComponent<BowlingPuzzle>();
    }

    void Update()
    {
        // (Opsional) Jika ingin animasi di-update ketika sudah goal sebelum script di-disable.
        if (isGoalSet)
        {
            // Pastikan hanya satu tween yang jalan pada frame pertama setelah isGoalSet = true
            // (Aman karena script akan di-disable di OnTriggerStay setelah tween utama)
            LeanTween.moveY(bridge, moveYPos, 1f)
                     .setEaseInOutSine()
                     .setOnComplete(() =>
                     {
                         Debug.Log("Bridge moved up!");
                     });
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (!isGoalSet && other.gameObject.CompareTag("Pickupable"))
        {
            // 1) Lock supaya hanya sekali
            isGoalSet = true;

            // 2) Hancurkan/crate selesai (sesuai logic kamu)
            if (crate != null) crate.Die();

            // 3) Bekukan bola
            GameObject bowlingBall = other.gameObject;
            Rigidbody ballRb = bowlingBall.GetComponent<Rigidbody>();
            if (ballRb != null)
            {
                ballRb.isKinematic = true;
                ballRb.linearVelocity = Vector3.zero;   // Unity 6 / Netcode
                ballRb.angularVelocity = Vector3.zero;
                // Jika pakai Unity versi lama:
                // ballRb.velocity = Vector3.zero;
                // ballRb.angularVelocity = Vector3.zero;
            }

            // 4) Posisikan & parent ke trigger
            bowlingBall.transform.position = this.transform.position;
            bowlingBall.transform.SetParent(this.transform, true);

            // 5) Jalankan animasi jembatan sekali
            LeanTween.moveY(bridge, moveYPos, 1.38f)
                     .setEaseInOutSine()
                     .setOnComplete(() =>
                     {
                         Debug.Log("Goal reached! Bridge moved up!");
                         bowlingPuzzle.enabled = false; // stop update & cegah trigger ulang
                     });

            // 6) SFX
            SoundManager.Instance?.PlaySound(triggerSFX, 0f, 0f, false, 1f);

            // 7) Panggil event kustom untuk dipasang via Inspector
            onGoalSet?.Invoke();
        }
    }
}
