using cowsins;
using UnityEngine;

public class BowlingPuzzle : MonoBehaviour
{
    [Header("Bowling Setting")]
    [SerializeField] Transform goalPos;
    [SerializeField] bool isGoalSet = false;

    [Header("Goals Setting")]
    [SerializeField] GameObject bridge;
    [SerializeField] float moveYPos;
    [SerializeField] AudioClip triggerSFX;

    [Header("Script Reference")]
    [SerializeField] Crate crate;

    // Script Reference
    private BowlingPuzzle bowlingPuzzle;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        bowlingPuzzle = this.GetComponent<BowlingPuzzle>();
    }

    // Update is called once per frame
    void Update()
    {
        if (isGoalSet == true)
        {
            LeanTween.moveY(bridge, moveYPos, 1f).setEaseInOutSine().setOnComplete(() =>
            {
                Debug.Log("Bridge moved up!");
            });
        }
    }

    private void OnTriggerStay(Collider other)
    {
        
        if (!isGoalSet && other.gameObject.CompareTag("Pickupable"))
        {
            // 1. Tandai puzzle sebagai selesai agar kode ini tidak berjalan lagi
            isGoalSet = true;

            crate.Die();

            GameObject bowlingBall = other.gameObject;
            Rigidbody ballRb = bowlingBall.GetComponent<Rigidbody>();

            // 2. Matikan fisika bola agar berhenti total (stuck)
            if (ballRb != null)
            {
                ballRb.isKinematic = true;
                
            }

            // 3. Posisikan bola tepat di tengah objek trigger ini
            bowlingBall.transform.position = this.transform.position;

            // 4. Jadikan bola sebagai child dari objek ini
            bowlingBall.transform.SetParent(this.transform);

            // 5. Jalankan animasi jembatan HANYA SEKALI
            LeanTween.moveY(bridge, moveYPos, 1.38f).setEaseInOutSine().setOnComplete(() =>
            {
                Debug.Log("Goal reached! Bridge moved up!");
                bowlingPuzzle.enabled = false; // Disable the script to prevent further updates
            });

            //6. Mainkan efek suara
            SoundManager.Instance.PlaySound(triggerSFX, 0f, 0f, false, 1f);
        }
    }
}
