/// <summary>
/// This script belongs to cowsins™ as a part of the cowsins´ FPS Engine. All rights reserved. 
/// </summary>
using UnityEngine;
using System.Collections;
namespace cowsins
{
    /// <summary>
    /// Inheriting from Interactable, this means you can interact with the door
    /// Keep in mind that this is highly subject to change on future updates
    /// </summary>
    [RequireComponent(typeof(BoxCollider))] // Require a trigger collider to detect side
    public partial class DoorInteractable : Interactable
    {
        [SerializeField, Title("DOOR INTERACTABLE", upMargin: 8, divider: true)] private string openInteractionText;

        [SerializeField] private string closeInteractionText;

        [SerializeField] private string lockedInteractionText;

        [SerializeField] private bool isLocked;

        [Tooltip("The pivot point for the door"), SerializeField]
        private Transform doorPivot;

        [Tooltip("The Vector3 to add to the door on opened"), SerializeField] private Vector3 offsetPosition;

        [Tooltip("How much you want to rotate the door"), SerializeField]
        private float openedDoorRotation;

        [Tooltip("rotation speed"), SerializeField]
        private float speed;

        [SerializeField] private AudioClip openDoorSFX, closeDoorSFX, lockedDoorSFX;

        private Quaternion closedRot;

        private Vector3 initialPos;

        private PlayerMovement playerMovement;

        private int side;

        private Coroutine doorCoroutine;

        private void Start()
        {
            // Initial settings
            initialPos = doorPivot.position;
            closedRot = doorPivot.rotation;
            playerMovement = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerMovement>();
            interactText = openInteractionText;
        }
 
        /// <summary>
        /// Check for interaction. Overriding from Interactable.cs
        /// </summary>
        public override void Interact(Transform player)
        {
            // Check if its locked
            if (isLocked)
            {
                SoundManager.Instance.PlaySound(lockedDoorSFX, 0, .1f, true, 0);
                return;
            }
            // Change state
            interacted = !interacted;

            // Display appropriate UI
            interactText = isLocked ? lockedInteractionText :
                (interacted ? closeInteractionText : openInteractionText);

            if (interacted) SoundManager.Instance.PlaySound(openDoorSFX, 0, .1f, true, 0);
            else SoundManager.Instance.PlaySound(closeDoorSFX, 0, .1f, true, 0);

            // Checking the side where we are opening the door from;
            side = (Vector3.Dot(transform.right, playerMovement.orientation.forward) > 0) ? 1 : -1;

            // Stop any running coroutines before starting a new one
            if (doorCoroutine != null)
                StopCoroutine(doorCoroutine);

            // Start coroutine for opening/closing the door
            doorCoroutine = StartCoroutine(HandleDoorMovement());

            interactableEvents.OnInteract?.Invoke();
        }

        private IEnumerator HandleDoorMovement()
        {
            Vector3 targetPos = interacted ? initialPos + offsetPosition : initialPos;
            Quaternion targetRot = interacted
                ? Quaternion.Euler(new Vector3(doorPivot.localRotation.x, openedDoorRotation * side, doorPivot.localRotation.z))
                : closedRot;

            // Smoothly move and rotate the door until it reaches the target position and rotation
            while (Vector3.Distance(doorPivot.position, targetPos) > 0.01f || Quaternion.Angle(doorPivot.rotation, targetRot) > 0.1f)
            {
                doorPivot.position = Vector3.Lerp(doorPivot.position, targetPos, Time.deltaTime * speed);
                doorPivot.rotation = Quaternion.Lerp(doorPivot.rotation, targetRot, Time.deltaTime * speed);
                yield return null;
            }

            // Ensure the door is exactly at the target position and rotation
            doorPivot.position = targetPos;
            doorPivot.rotation = targetRot;
        }

        public void Lock() => isLocked = true;

        public void UnLock() => isLocked = false;

        public void ToggleLock() => isLocked = !isLocked;
    }
}
