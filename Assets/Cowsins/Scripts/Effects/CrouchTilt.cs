/// <summary>
/// This script belongs to cowsins™ as a part of the cowsins´ FPS Engine. All rights reserved. 
/// </summary>
using UnityEngine;
using System.Collections;

namespace cowsins
{
    public class CrouchTilt : MonoBehaviour
    {
        [Tooltip("Rotation desired when crouching"), SerializeField] private Vector3 tiltRot, tiltPosOffset;
        [Tooltip("Tilting / Rotation velocity"), SerializeField] private float tiltSpeed;

        [HideInInspector] public PlayerMovement player;
        [HideInInspector] public WeaponController wp;

        private Quaternion origRot;
        private Vector3 origPos;
        private Coroutine tiltCoroutine;

        void Start()
        {
            player = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerMovement>();
            wp = GameObject.FindGameObjectWithTag("Player").GetComponent<WeaponController>();

            origRot = transform.localRotation;
            origPos = transform.localPosition;

            player.events.OnCrouch.AddListener(StartCrouch);
            player.events.OnStopCrouch.AddListener(StopCrouch);
        }

        private void OnDisable()
        {
            player?.events.OnCrouch.RemoveListener(StartCrouch);
            player?.events.OnStopCrouch.RemoveListener(StopCrouch);
        }

        private void StartCrouch()
        {
            if (!wp.isAiming) StartTilting(tiltRot, origPos + tiltPosOffset);
        }

        private void StopCrouch()
        {
            StartTilting(origRot.eulerAngles, origPos);
        }

        private void StartTilting(Vector3 targetRotation, Vector3 targetPosition)
        {
            if (tiltCoroutine != null) StopCoroutine(tiltCoroutine);
            tiltCoroutine = StartCoroutine(TiltRoutine(targetRotation, targetPosition));
        }

        private IEnumerator TiltRoutine(Vector3 targetRotation, Vector3 targetPosition)
        {
            while (Quaternion.Angle(transform.localRotation, Quaternion.Euler(targetRotation)) > 0.1f ||
                   Vector3.Distance(transform.localPosition, targetPosition) > 0.01f)
            {
                transform.localRotation = Quaternion.Lerp(transform.localRotation, Quaternion.Euler(targetRotation), Time.deltaTime * tiltSpeed);
                transform.localPosition = Vector3.Lerp(transform.localPosition, targetPosition, Time.deltaTime * tiltSpeed);
                yield return null;  // Wait for the next frame before continuing
            }
        }
    }
}
