/// <summary>
/// This script belongs to cowsins™ as a part of the cowsins´ FPS Engine. All rights reserved. 
/// </summary>
using UnityEngine.UI;
using UnityEngine;
using UnityEngine.Events;
namespace cowsins
{
    public abstract class Pickeable : Interactable
    {
        [System.Serializable]
        public class Events
        {
            public UnityEvent OnPickUp;
        }
        [SerializeField] private Events events;

        [Tooltip("Apply the selected effect")]
        public bool rotates, translates;

        [Tooltip("Change the speed of the selected effect"), SerializeField]
        private float rotationSpeed, translationSpeed;

        [SerializeField] protected Image image;

        [Tooltip("Transform under which the graphics will be stored at when instantiated"), SerializeField]
        protected Transform graphics;

        [HideInInspector] public bool dropped;

        [HideInInspector] protected bool pickeable;

        private Transform obj;

        private float timer = 0f;

        private Rigidbody rb;

        public virtual void Awake()
        {
            pickeable = false;
            obj = transform.Find("Graphics");
            rb = GetComponent<Rigidbody>();
        }
        private void Update()
        {
            if (rotates || translates) Movement();
        }

        public override void Interact(Transform player) => events.OnPickUp.Invoke();

        /// <summary>
        /// Apply effects, usually for more cartoony, stylized, anime approaches
        /// </summary>
        private void Movement()
        {
            rb?.AddForce(Vector3.down * 15, ForceMode.Force);
            if (obj == null) return;

            // Rotation
            if (rotates)
                obj.Rotate(Vector3.up * rotationSpeed * Time.deltaTime);

            // Translation (Up and Down Motion)
            if (translates)
            {
                timer += Time.deltaTime * translationSpeed;
                float translateMotion = Mathf.Sin(timer) / 7000f;
                Vector3 localPos = obj.localPosition;
                localPos.y += translateMotion;
                obj.localPosition = localPos;
            }
        }

        public virtual void Drop(WeaponController wcon, Transform orientation)
        {
            dropped = true;

            Vector3 force = orientation.forward * 4;

            if (rb == null)
            {
                Debug.LogError("<color=red>[COWSINS]</color> <b><color=yellow>Rigidbody component not found!</color></b> " +
                    "Please assign a <b><color=cyan>Rigidbody Component</color></b> to your Pickeable Object to fix this error.", this);
            }
            else rb.AddForce(force, ForceMode.VelocityChange);
        }

        public virtual void DestroyGraphics() => Destroy(graphics.transform.GetChild(0).gameObject);
    }
}