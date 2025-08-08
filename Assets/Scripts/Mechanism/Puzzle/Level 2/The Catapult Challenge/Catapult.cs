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

    // Variabel armCollider tidak lagi diperlukan dari Inspector
    // [Tooltip("Collider dari lengan katapel untuk diabaikan saat melontar.")]
    // public Collider armCollider; 

    [Header("Throw Settings")]
    [Tooltip("Throw force, this value can be splitted by the mass of the object.")]
    public float launchForce = 5000f;
    [Tooltip("Launch Angle with a degree (0 = Straight Forward, 90 = Straight Upwards).")]
    [Range(0f, 90f)]
    public float launchAngle = 45f;

    [Header("Arm Animation")]
    [Tooltip("Arm first position (a ready position to launch)")]
    public float startAngle = -45f;
    [Tooltip("Arm final position (a position after throwing an object)")]
    public float endAngle = 45f;
    [Tooltip("Arm speed Animation when launching/throwing an object")]
    public float animationSpeed = 5f;

    private bool isLaunching = false;
    private Quaternion startRotation;
    private Quaternion endRotation;

    //Script Reference
    [Header("Player Reference")]
    [SerializeField] PickupSystem pickupSystem;

    private void Start()
    {
        startRotation = Quaternion.Euler(startAngle, 0, 0);
        endRotation = Quaternion.Euler(endAngle, 0, 0);
        if (arm != null) arm.localRotation = startRotation;
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

        // Cek dulu apakah objek yang masuk memiliki komponen Interactable
        Interactable interactableObject = other.GetComponent<Interactable>();
        if (interactableObject == null) return; // Jika bukan interactable, abaikan

        // Jika katapel masih kosong, baru proses objeknya
        if (objectToLaunch == null)
        {
            Rigidbody rb = other.GetComponent<Rigidbody>();
            if (rb != null)
            {
                // Simpan referensi Rigidbody-nya
                objectToLaunch = rb;

                // Nonaktifkan skrip Interactable agar tidak bisa diambil lagi oleh pemain
                interactableObject.enabled = false;

                // Lakukan operasi fisika pada Rigidbody
                objectToLaunch.isKinematic = true;
                objectToLaunch.transform.position = launchPoint.position;
                objectToLaunch.transform.rotation = launchPoint.rotation;

                Debug.Log(other.name + " ditempatkan di katapel. Script Interactable dinonaktifkan.");
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

            // --- PERBAIKAN UTAMA DI SINI ---
            // Dapatkan SEMUA collider dari lengan dan anak-anaknya
            Collider[] allArmColliders = arm.GetComponentsInChildren<Collider>();

            // Abaikan tabrakan antara proyektil dan semua bagian lengan
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

            // Setelah beberapa saat, aktifkan kembali tabrakannya
            StartCoroutine(ReenableCollision(objectCollider, allArmColliders, 1.0f));

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

    // Coroutine diperbarui untuk menerima array of colliders
    private IEnumerator ReenableCollision(Collider objCol, Collider[] armCols, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (objCol != null && armCols != null)
        {
            foreach (Collider armCol in armCols)
            {
                if (armCol != null) // Cek null untuk keamanan
                {
                    Physics.IgnoreCollision(objCol, armCol, false);
                }
            }
        }
    }
}