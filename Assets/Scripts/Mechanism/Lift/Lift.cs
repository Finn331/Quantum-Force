using UnityEngine;
using System.Collections;
using cowsins; // Hapus atau ganti baris ini jika Anda tidak menggunakan namespace 'cowsins'

public class Lift : MonoBehaviour
{
    [Header("Lift Settings")]
    [Tooltip("The destination point where the player will be teleported.")]
    [SerializeField] Transform teleportPoint;
    [SerializeField] GameObject liftDoorDestination;
    [SerializeField] float liftDoorPosY;

    [Header("Transition Settings")]
    [Tooltip("The UI Panel with a blur or black image that will fade in and out.")]
    [SerializeField] GameObject liftUIBlur;
    [Tooltip("The main UI for the lift that appears after the transition.")]
    [SerializeField] GameObject liftUI;
    [Tooltip("How long the fade in and fade out animations take.")]
    [SerializeField] float fadeDuration = 0.5f;

    // --- VARIABEL BARU ---
    [Tooltip("Jeda singkat setelah teleport sebelum layar kembali jernih (dalam detik).")]
    [SerializeField] float postTeleportDelay = 0.5f;

    private CanvasGroup blurCanvasGroup;
    private bool isTransitioning = false;

    private void Awake()
    {
        if (liftUIBlur != null)
        {
            blurCanvasGroup = liftUIBlur.GetComponent<CanvasGroup>();
            if (blurCanvasGroup == null)
            {
                blurCanvasGroup = liftUIBlur.AddComponent<CanvasGroup>();
            }
        }
    }

    void Start()
    {
        if (liftUIBlur != null)
        {
            blurCanvasGroup.alpha = 0;
            liftUIBlur.SetActive(false);
        }
        if (liftUI != null) liftUI.SetActive(false);
    }

    public void UseLift()
    {
        if (isTransitioning) return;
        if (teleportPoint == null)
        {
            Debug.LogError("Teleport Point is not set on the Lift!", gameObject);
            return;
        }
        StartCoroutine(LiftTransition());
    }

    private IEnumerator LiftTransition()
    {
        isTransitioning = true;

        // --- FADE IN ---
        if (liftUIBlur != null)
        {
            liftUIBlur.SetActive(true);
            LeanTween.alphaCanvas(blurCanvasGroup, 1f, fadeDuration).setEase(LeanTweenType.linear);
        }

        yield return new WaitForSeconds(fadeDuration);

        // --- TELEPORT PLAYER ---
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            PlayerMovement playerMovement = player.GetComponent<PlayerMovement>();
            CharacterController cc = player.GetComponent<CharacterController>();
            Rigidbody rb = player.GetComponent<Rigidbody>();

            if (playerMovement != null) playerMovement.enabled = false;
            if (cc != null) cc.enabled = false;
            if (rb != null) rb.isKinematic = true;

            player.transform.position = teleportPoint.position;
            // player.transform.rotation = teleportPoint.rotation; // Anda bisa aktifkan ini jika perlu

            yield return null;

            if (rb != null) rb.isKinematic = false;
            if (cc != null) cc.enabled = true;
            if (playerMovement != null) playerMovement.enabled = true;
        }
        else
        {
            Debug.LogError("Player not found!", gameObject);
        }

        if (liftUI != null) liftUI.SetActive(true);

        // --- PERBAIKAN DI SINI ---
        // Beri jeda singkat SETELAH teleportasi
        yield return new WaitForSeconds(postTeleportDelay);

        // --- FADE OUT ---
        if (liftUIBlur != null)
        {
            LeanTween.alphaCanvas(blurCanvasGroup, 0f, fadeDuration).setEase(LeanTweenType.linear).setOnComplete(() =>
            {
                liftUIBlur.SetActive(false);
                // Buka pintu Lift destination
                LeanTween.moveY(liftDoorDestination, liftDoorPosY, 0.5f).setEase(LeanTweenType.easeInOutQuad);
            });
        }

        yield return new WaitForSeconds(fadeDuration);

        isTransitioning = false;
    }
}