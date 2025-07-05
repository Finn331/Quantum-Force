using UnityEngine;

public class PickupSystem : MonoBehaviour
{
    public float pickupRange = 3f;
    public Transform holdPoint;
    public float throwForce = 500f;

    [Header("Weapon Check")]
    public Transform weaponHolster; // drag weapon holster di inspector

    private GameObject heldObject;
    private Rigidbody heldRb;
    private Collider heldCollider;

    void Update()
    {
        // Example Input Pickup Key
        //if (Input.GetKeyDown(KeyCode.E))
        //{
        //    if (heldObject == null)
        //    {
        //        TryPickup();
        //    }
        //}

        if (Input.GetKeyDown(KeyCode.G))
        {
            if (heldObject != null)
            {
                ThrowObject();
            }
        }

        if (heldObject != null)
        {
            heldObject.transform.position = holdPoint.position;
        }
    }

    public void TryPickup()
    {
        // Check if player is holding a weapon
        if (weaponHolster != null && weaponHolster.childCount > 0)
        {
            bool hasActiveWeapon = false;

            foreach (Transform child in weaponHolster)
            {
                if (child.gameObject.activeInHierarchy)
                {
                    hasActiveWeapon = true;
                    break;
                }
            }

            if (hasActiveWeapon)
            {
                Debug.Log("Cannot pick up object while holding a weapon.");
                return; // Prevent pickup if a weapon is currently held
            }
        }

        Camera cam = Camera.main;
        Ray ray = cam.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2));
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, pickupRange))
        {
            if (hit.collider.CompareTag("Pickupable"))
            {
                heldObject = hit.collider.gameObject;
                heldRb = heldObject.GetComponent<Rigidbody>();

                if (heldRb != null)
                {
                    heldRb.useGravity = false;
                    heldRb.isKinematic = true;
                    heldCollider = heldObject.GetComponent<Collider>();

                    if (heldCollider != null)
                        heldCollider.enabled = false;
                }

                heldObject.transform.position = holdPoint.position;
                heldObject.transform.SetParent(holdPoint);
            }
        }
    }

    void ThrowObject()
    {
        heldObject.transform.SetParent(null);

        if (heldRb != null)
        {
            heldRb.isKinematic = false;
            heldRb.useGravity = true;

            Camera cam = Camera.main;
            Vector3 throwDirection = (cam != null ? cam.transform.forward : transform.forward) + Vector3.up * 0.2f;
            heldRb.AddForce(throwDirection.normalized * throwForce);

            if (heldCollider != null)
                heldCollider.enabled = true;
        }

        heldObject = null;
        heldRb = null;
    }
}
