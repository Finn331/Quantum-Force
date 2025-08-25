using UnityEngine;
using System.Collections;
using cowsins;

public class Lift : MonoBehaviour
{
    [Header("Lift Settings")]
    [SerializeField] private Transform teleportPoint;
    [SerializeField] private GameObject liftDoorDestination;
    [SerializeField] private float liftDoorPosY;

    [Header("Transition Settings")]
    [SerializeField] private GameObject liftUIBlur;
    [SerializeField] private GameObject liftUI;
    [SerializeField] private float fadeDuration = 0.5f;
    [SerializeField] private float postTeleportDelay = 0.5f;

    [Header("References (optional)")]
    [Tooltip("(Opsional) Drag PlayerMovement di sini. Jika kosong, akan dicari otomatis via tag Player.")]
    [SerializeField] private PlayerMovement playerMovementRef;

    private CanvasGroup blurCanvasGroup;
    private bool isTransitioning;

    private void Awake()
    {
        if (liftUIBlur != null)
        {
            blurCanvasGroup = liftUIBlur.GetComponent<CanvasGroup>();
            if (blurCanvasGroup == null) blurCanvasGroup = liftUIBlur.AddComponent<CanvasGroup>();
        }
    }

    private void Start()
    {
        if (liftUIBlur != null)
        {
            blurCanvasGroup.alpha = 0f;
            liftUIBlur.SetActive(false);
        }
        if (liftUI != null) liftUI.SetActive(false);

        if (playerMovementRef == null)
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player) playerMovementRef = player.GetComponent<PlayerMovement>();
        }
    }

    public void UseLift()
    {
        if (isTransitioning) return;

        if (teleportPoint == null)
        {
            Debug.LogError("Teleport Point is not set on the Lift!", gameObject);
            return;
        }
        if (playerMovementRef == null)
        {
            Debug.LogError("PlayerMovement not found. Assign it in the inspector or ensure Player has the 'Player' tag.", gameObject);
            return;
        }

        StartCoroutine(LiftTransition());
    }

    private IEnumerator LiftTransition()
    {
        isTransitioning = true;

        // Fade In
        if (liftUIBlur != null)
        {
            liftUIBlur.SetActive(true);
            LeanTween.alphaCanvas(blurCanvasGroup, 1f, fadeDuration).setEase(LeanTweenType.linear);
        }
        yield return new WaitForSeconds(fadeDuration);

        // --- HITUNG ROTASI MENGHADAP PINTU ---
        Quaternion faceDoorRotation = teleportPoint.rotation; // default: arah spawn point
        if (liftDoorDestination != null)
        {
            Vector3 toDoor = liftDoorDestination.transform.position - teleportPoint.position;
            toDoor.y = 0f; // hanya arah horizontal
            if (toDoor.sqrMagnitude > 0.0001f)
                faceDoorRotation = Quaternion.LookRotation(toDoor.normalized, Vector3.up);
        }

        // Teleport + set arah menghadap pintu (gunakan fungsi PlayerMovement)
        playerMovementRef.TeleportPlayer(teleportPoint.position, faceDoorRotation);

        if (liftUI != null) liftUI.SetActive(true);

        // Jeda singkat setelah teleport
        yield return new WaitForSeconds(postTeleportDelay);

        // Fade Out & buka pintu tujuan
        if (liftUIBlur != null)
        {
            LeanTween.alphaCanvas(blurCanvasGroup, 0f, fadeDuration).setEase(LeanTweenType.linear).setOnComplete(() =>
            {
                liftUIBlur.SetActive(false);
                if (liftUI != null) liftUI.SetActive(false);

                if (liftDoorDestination != null)
                    LeanTween.moveY(liftDoorDestination, liftDoorPosY, 0.5f).setEase(LeanTweenType.easeInOutQuad);
            });
        }
        yield return new WaitForSeconds(fadeDuration);

        isTransitioning = false;
    }
}
