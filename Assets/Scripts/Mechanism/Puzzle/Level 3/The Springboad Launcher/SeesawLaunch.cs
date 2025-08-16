using UnityEngine;
using System.Collections;
using cowsins;

public class SeesawLaunch : MonoBehaviour
{
    [Header("Launch Settings")]
    [Tooltip("The target destination the player will be launched towards.")]
    public Transform launchTargetPoint;

    [Tooltip("The time it takes for the player to travel to the destination.")]
    public float launchDuration = 2.0f;

    [Tooltip("The peak height of the launch arc, relative to the starting point.")]
    public float launchHeight = 5f;

    [Tooltip("The layer for heavy objects that can act as a trigger.")]
    public LayerMask fallenObjectLayer;

    [Header("Player Settings")]
    [Tooltip("The exact name of the movement script on your Player (case-sensitive).")]
    public string playerMovementScriptName;

    [Header("SFX")]
    public AudioClip launchSFX;
    private AudioSource audioSource;

    // Internal variables
    private Rigidbody playerRigidbody;
    private bool isPlayerOnEnd = false;
    private bool isLaunching = false;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }

    public void OnPlayerEnter(Rigidbody playerRb)
    {
        playerRigidbody = playerRb;
        isPlayerOnEnd = true;
        Debug.Log("Player is in the launch position.");
    }

    public void OnPlayerExit()
    {
        if (!isLaunching)
        {
            playerRigidbody = null;
            isPlayerOnEnd = false;
            Debug.Log("Player has left the launch position.");
        }
    }

    public void OnWeightEnter(Collider weightCollider)
    {
        if (!isLaunching && fallenObjectLayer == (fallenObjectLayer | (1 << weightCollider.gameObject.layer)))
        {
            if (isPlayerOnEnd && playerRigidbody != null)
            {
                StartCoroutine(LaunchSequence(playerRigidbody));
            }
        }
    }

    private IEnumerator LaunchSequence(Rigidbody rbToLaunch)
    {
        isLaunching = true;
        Debug.Log("Weight has landed! Starting cinematic launch...");

        // Matikan kontrol pemain dan fisika standar
        MonoBehaviour playerScript = null;
        if (!string.IsNullOrEmpty(playerMovementScriptName))
        {
            playerScript = rbToLaunch.GetComponent(playerMovementScriptName) as MonoBehaviour;
            if (playerScript != null) playerScript.enabled = false;
        }

        rbToLaunch.isKinematic = true;
        rbToLaunch.useGravity = false;

        if (launchSFX != null) audioSource.PlayOneShot(launchSFX);

        // --- LOGIKA LONTARAN BARU DENGAN LEANTWEEN ---

        Vector3 startPos = rbToLaunch.position;
        Vector3 endPos = launchTargetPoint.position;

        // Animasikan pergerakan X dan Z secara linear (kecepatan konstan)
        LeanTween.moveX(rbToLaunch.gameObject, endPos.x, launchDuration);
        LeanTween.moveZ(rbToLaunch.gameObject, endPos.z, launchDuration);

        // Animasikan pergerakan Y dalam bentuk parabola
        // Pertama, naik ke puncak, lalu turun ke tujuan
        LeanTween.sequence()
            .append(LeanTween.moveY(rbToLaunch.gameObject, startPos.y + launchHeight, launchDuration / 2).setEaseOutQuad())
            .append(LeanTween.moveY(rbToLaunch.gameObject, endPos.y, launchDuration / 2).setEaseInQuad());


        // Tunggu hingga durasi lontaran selesai
        yield return new WaitForSeconds(launchDuration);

        // --- KEMBALIKAN KONTROL PEMAIN ---
        Debug.Log("Player has arrived at the destination.");
        if (rbToLaunch != null)
        {
            rbToLaunch.isKinematic = false;
            rbToLaunch.useGravity = true;
            rbToLaunch.linearVelocity = Vector3.zero;

            if (playerScript != null)
            {
                playerScript.enabled = true;
            }
        }
        isLaunching = false;
    }
}