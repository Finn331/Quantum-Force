using UnityEngine;

public class PickupSystem : MonoBehaviour
{
    public float pickupRange = 3f;
    public Transform holdPoint;
    public float throwForce = 500f;

    private GameObject heldObject;
    private Rigidbody heldRb;
    private Collider heldCollider;

    void Update()
    {
        //// Input for pickup ("E" or Controller Button X)
        //if (Input.GetKeyDown(KeyCode.E)/* || Input.GetButtonDown("Pickup")*/)
        //{
        //    if (heldObject == null)
        //    {
        //        TryPickup();
        //    }
        //}

        // Input for throw ("G" or Controller Button B)
        if (Input.GetKeyDown(KeyCode.G)/* || Input.GetButtonDown("drop")*/)
        {
            if (heldObject != null)
            {
                ThrowObject();
            }
        }

        // Keep object at hold point if holding
        if (heldObject != null)
        {
            heldObject.transform.position = holdPoint.position;
        }
    }

    public void TryPickup()
    {
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

                    // Disable Collideer
                    if (heldCollider != null)
                    {
                        heldCollider.enabled = false; // Disable collider while holding
                    }                   
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
            if (cam != null)
            {
                // Add a slight upward component to the throw direction
                Vector3 throwDirection = cam.transform.forward + cam.transform.up * 0.2f;
                heldRb.AddForce(throwDirection.normalized * throwForce);

                // Turning on Collider
                heldCollider = heldObject.GetComponent<Collider>();
                if (heldCollider != null)
                {
                    heldCollider.enabled = true; // Disable collider while holding
                }
            }
            else
            {
                // Fallback if no camera
                Vector3 throwDirection = transform.forward + transform.up * 0.2f;
                heldRb.AddForce(throwDirection.normalized * throwForce);
            }
        }

        heldObject = null;
        heldRb = null;
    }


}
