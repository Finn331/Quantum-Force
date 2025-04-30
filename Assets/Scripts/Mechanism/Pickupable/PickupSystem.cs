using UnityEngine;
using UnityEngine.InputSystem;

public class PickupSystem : MonoBehaviour
{
    [Header("Pickup Settings")]
    public float pickupRange = 2f;
    public Transform holdPosition;

    private GameObject heldObject = null;
    private Rigidbody heldRigidbody = null;

    private PlayerInput playerInput;
    private InputAction interactAction;

    private void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
        interactAction = playerInput.actions["Interact"]; // Make sure you have an "Interact" action
        interactAction.performed += context => HandleInteract();
    }

    private void OnDestroy()
    {
        interactAction.performed -= context => HandleInteract();
    }

    private void HandleInteract()
    {
        if (heldObject == null)
        {
            TryPickupObject();
        }
        else
        {
            DropHeldObject();
        }
    }

    private void TryPickupObject()
    {
        Ray ray = new Ray(transform.position, transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, pickupRange))
        {
            GameObject target = hit.collider.gameObject;

            if (target.CompareTag("Pickupable"))
            {
                Rigidbody targetRb = target.GetComponent<Rigidbody>();
                if (targetRb != null)
                {
                    heldObject = target;
                    heldRigidbody = targetRb;

                    heldRigidbody.isKinematic = true;
                    heldRigidbody.useGravity = false;

                    heldObject.transform.position = holdPosition.position;
                    heldObject.transform.rotation = holdPosition.rotation;
                    heldObject.transform.SetParent(holdPosition);
                }
            }
        }
    }

    private void DropHeldObject()
    {
        if (heldObject != null)
        {
            heldObject.transform.SetParent(null);
            heldRigidbody.isKinematic = false;
            heldRigidbody.useGravity = true;

            heldObject = null;
            heldRigidbody = null;
        }
    }
}
