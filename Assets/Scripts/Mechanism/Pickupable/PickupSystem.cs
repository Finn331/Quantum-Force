using UnityEngine;
using cowsins; // Hapus atau ganti baris ini jika Anda tidak menggunakan namespace 'cowsins'

public class PickupSystem : MonoBehaviour
{
    public float pickupRange = 3f;
    public Transform holdPoint;
    public float throwForceMultiplier = 100f;

    [Header("Physics Settings")]
    [Tooltip("Seberapa kuat objek 'tertarik' ke titik pegang.")]
    public float holdSpeed = 20f;

    [Header("Weapon Check")]
    public Transform weaponHolster;
    [SerializeField] GameObject handFullUI;
    [SerializeField] GameObject handFullText;

    private GameObject heldObject;
    private Rigidbody heldRb;
    private Collider playerCollider; // Variabel baru untuk menyimpan collider pemain

    private void Awake()
    {
        // Dapatkan komponen collider dari pemain saat game dimulai
        playerCollider = GetComponent<Collider>();
    }

    private void FixedUpdate()
    {
        if (heldObject != null)
        {
            MoveObjectWithPhysics();
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.G))
        {
            if (heldObject != null)
            {
                ThrowObject();
            }
        }
    }

    public void TryPickup()
    {
        if (IsHoldingWeapon())
        {
            Debug.Log("Cannot pick up object while holding a weapon.");
            handFullUI.SetActive(true);
            LeanTween.scale(handFullUI, new Vector3(1.5f, 1.5f, 1.5f), 2f).setEase(LeanTweenType.easeOutBounce).setOnComplete(() =>
            {
                LeanTween.scale(handFullUI, Vector3.one, 1.5f).setEase(LeanTweenType.easeInBounce).setOnComplete(() =>
                {
                    handFullUI.SetActive(false);                   
                });
            });
            return;
        }

        Camera cam = Camera.main;
        Ray ray = cam.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2));
        if (Physics.Raycast(ray, out RaycastHit hit, pickupRange))
        {
            if (hit.collider.TryGetComponent(out Pickupable pickupableObject))
            {
                heldObject = pickupableObject.gameObject;
                heldRb = heldObject.GetComponent<Rigidbody>();
                Collider heldCollider = heldObject.GetComponent<Collider>();

                if (heldRb != null)
                {
                    heldRb.useGravity = true;
                    heldRb.freezeRotation = true;
                    heldRb.collisionDetectionMode = CollisionDetectionMode.Continuous;
                }

                // --- PERBAIKAN UTAMA DI SINI ---
                // Abaikan tabrakan antara collider pemain dan collider objek yang dipegang
                if (heldCollider != null && playerCollider != null)
                {
                    Physics.IgnoreCollision(playerCollider, heldCollider, true);
                }
            }
        }
    }

    private void MoveObjectWithPhysics()
    {
        Vector3 moveDirection = (holdPoint.position - heldRb.position);
        heldRb.linearVelocity = moveDirection * holdSpeed;
    }

    public void ThrowObject()
    {
        if (heldObject == null) return;

        Rigidbody rbToThrow = heldRb;
        Collider colToRestore = heldObject.GetComponent<Collider>();
        Pickupable itemProperties = heldObject.GetComponent<Pickupable>();

        RestoreObjectState(rbToThrow, colToRestore);

        if (rbToThrow != null)
        {
            float force = throwForceMultiplier / Mathf.Max(rbToThrow.mass, 0.1f);

            Camera cam = Camera.main;
            Vector3 throwDirection = cam != null ? cam.transform.forward : transform.forward;

            rbToThrow.AddForce(throwDirection * force, ForceMode.Impulse);

            if (itemProperties != null && itemProperties.shouldSpin)
            {
                Vector3 spinAxis = cam != null ? cam.transform.right : transform.right;
                rbToThrow.AddTorque(spinAxis * itemProperties.spinForce, ForceMode.Impulse);
            }
        }
    }

    void DropObject()
    {
        if (heldObject == null) return;
        RestoreObjectState(heldRb, heldObject.GetComponent<Collider>());
    }

    private void RestoreObjectState(Rigidbody rbToRestore, Collider colToRestore)
    {
        if (rbToRestore != null)
        {
            rbToRestore.freezeRotation = false;
            rbToRestore.collisionDetectionMode = CollisionDetectionMode.Discrete;
        }

        // --- PERBAIKAN UTAMA DI SINI ---
        // Aktifkan kembali tabrakan antara pemain dan objek
        if (colToRestore != null && playerCollider != null)
        {
            Physics.IgnoreCollision(playerCollider, colToRestore, false);
        }

        heldObject = null;
        heldRb = null;
    }

    private bool IsHoldingWeapon()
    {
        if (weaponHolster != null && weaponHolster.childCount > 0)
        {
            foreach (Transform child in weaponHolster)
            {
                if (child.gameObject.activeInHierarchy) return true;
            }
        }
        return false;
    }
}