using UnityEngine;

namespace cowsins
{
    public class CameraFOVManager : MonoBehaviour
    {
        [SerializeField] private Rigidbody player;

        private float baseFOV;
        private Camera cam;
        private PlayerMovement movement;
        private WeaponController weapon;
        private float targetFOV;
        private float lerpSpeed; 

        private void Start()
        {
            cam = GetComponent<Camera>();
            movement = player.GetComponent<PlayerMovement>();
            weapon = player.GetComponent<WeaponController>();

            baseFOV = movement.normalFOV; // Initialize baseFOV once in Start
            targetFOV = baseFOV; 
        }

        private void Update()
        {
            // Smoothly interpolate FOV towards the target value
            cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFOV, Time.deltaTime * lerpSpeed);
        }
        private bool AllowChangeFOV()
        {
            return weapon.isAiming && weapon.weapon != null;
        }

        public void SetFOV(float fov, float speed)
        {
            if (AllowChangeFOV())
                return; // Not applicable if aiming
            targetFOV = fov;
            lerpSpeed = speed;  
        }

        public void SetFOV(float fov)
        {
            if (AllowChangeFOV())
                return; // Not applicable if aiming
            targetFOV = fov;
            lerpSpeed = movement.fadeInFOVAmount; 
        }

        public void ForceAddFOV(float fov)
        {
            cam.fieldOfView -= fov;
            lerpSpeed = movement.fadeInFOVAmount;
        }
    }
}
