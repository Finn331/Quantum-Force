using UnityEngine;
using System.Collections;
using cowsins;

public class Catapult : MonoBehaviour
{
    [Header("Component Reference")]
    [Tooltip("Catapult arm for launching an object.")]
    public Transform arm;
    [Tooltip("Launch Point for placing an object/ammo")]
    public Transform launchPoint;
    [Tooltip("Object to be throw and it can be automatically choose or manualy.")]
    public Rigidbody objectToLaunch;

    [Header("Throw Settings")]
    [Tooltip("Throw force, this value can be splitted by the mass of the object.")]
    public float launchForce = 5000f;
    [Tooltip("Launch Angle with a degree (0 = Straight Forward, 90 = Straight Upwards).")]
    [Range(0f, 90f)]
    public float launchAngle = 45f;

    [Header("Arm Animation")]
    public float startAngle = -45f;
    public float endAngle = 45f;
    public float animationSpeed = 5f;

    private bool isLaunching = false;
    private Quaternion startRotation;
    private Quaternion endRotation;

    //Script Reference
    [Header("Player Reference")]
    [SerializeField] PickupSystem pickupSystem;

    // --- SFX ---
    [Header("SFX")]
    [SerializeField] private AudioClip loadSFX;
    [SerializeField] private AudioClip launchSFX;
    [Range(0f, 1f)][SerializeField] private float sfxVolume = 1f;

    private AudioSource audioSource;

    private void Start()
    {
        startRotation = Quaternion.Euler(startAngle, 0, 0);
        endRotation = Quaternion.Euler(endAngle, 0, 0);
        if (arm != null) arm.localRotation = startRotation;

        // Setup AudioSource
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1f; // 3D sound
        audioSource.volume = sfxVolume;
    }

    private void LateUpdate()
    {
        if (objectToLaunch != null && objectToLaunch.isKinematic)
        {
            objectToLaunch.transform.position = launchPoint.position;
            objectToLaunch.transform.rotation = launchPoint.rotation;
        }
    }

    public void OnObjectEnterTrigger(Collider other)
    {
        pickupSystem.ThrowObject();

        Interactable interactableObject = other.GetComponent<Interactable>();
        if (interactableObject == null) return;

        if (objectToLaunch == null)
        {
            Rigidbody rb = other.GetComponent<Rigidbody>();
            if (rb != null)
            {
                objectToLaunch = rb;

                interactableObject.enabled = false;

                objectToLaunch.isKinematic = true;
                objectToLaunch.transform.position = launchPoint.position;
                objectToLaunch.transform.rotation = launchPoint.rotation;

                Debug.Log(other.name + " ditempatkan di katapel.");

                // mainkan SFX load
                PlaySFX(loadSFX);
            }
        }
    }

    public void OnObjectExitTrigger(Collider other)
    {
        if (other.GetComponent<Rigidbody>() == objectToLaunch)
        {
            if (objectToLaunch != null) objectToLaunch.isKinematic = false;
            objectToLaunch = null;
        }
    }

    public bool IsReadyToLaunch()
    {
        return !isLaunching && objectToLaunch != null;
    }

    public void Launch()
    {
        if (!IsReadyToLaunch()) return;
        StartCoroutine(LaunchSequence());
    }

    private IEnumerator LaunchSequence()
    {
        isLaunching = true;

        float t = 0;
        while (t < 1)
        {
            t += Time.deltaTime * animationSpeed;
            if (arm != null) arm.localRotation = Quaternion.Slerp(startRotation, endRotation, t);
            yield return null;
        }

        if (objectToLaunch != null)
        {
            objectToLaunch.isKinematic = false;

            Collider objectCollider = objectToLaunch.GetComponent<Collider>();
            Collider[] allArmColliders = arm.GetComponentsInChildren<Collider>();

            if (objectCollider != null && allArmColliders.Length > 0)
            {
                foreach (Collider armPartCollider in allArmColliders)
                {
                    Physics.IgnoreCollision(objectCollider, armPartCollider, true);
                }
            }

            Vector3 launchDirection = Quaternion.AngleAxis(-launchAngle, transform.right) * transform.forward;
            float effectiveForce = launchForce / objectToLaunch.mass;
            objectToLaunch.AddForce(launchDirection * effectiveForce, ForceMode.Impulse);

            StartCoroutine(ReenableCollision(objectCollider, allArmColliders, 1.0f));

            // mainkan SFX launch
            PlaySFX(launchSFX);

            objectToLaunch = null;
        }

        yield return new WaitForSeconds(1f);

        t = 0;
        while (t < 1)
        {
            t += Time.deltaTime * animationSpeed;
            if (arm != null) arm.localRotation = Quaternion.Slerp(endRotation, startRotation, t);
            yield return null;
        }

        isLaunching = false;
    }

    private IEnumerator ReenableCollision(Collider objCol, Collider[] armCols, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (objCol != null && armCols != null)
        {
            foreach (Collider armCol in armCols)
            {
                if (armCol != null) Physics.IgnoreCollision(objCol, armCol, false);
            }
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
