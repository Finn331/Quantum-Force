using UnityEngine;
using UnityEngine.Events;

namespace cowsins
{
    public partial class Trigger : Identifiable
    {
        [System.Serializable]
        public class Events
        {
            public UnityEvent onEnter, onStay, onExit;
        }

        [SerializeField] private Events events;

        protected bool triggered;

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                events.onEnter?.Invoke();
                triggered = true;
                TriggerEnter(other);
#if SAVE_LOAD_ADD_ON
                SaveTrigger();
                TriggeredState(); 
#endif
            }
        }
        private void OnTriggerStay(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                events.onStay?.Invoke();
                TriggerStay(other);
            }
        }
        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                events.onExit?.Invoke();
                TriggerExit(other);
                triggered = false;
#if SAVE_LOAD_ADD_ON
                SaveData();
#endif
            }
        }

        public virtual void TriggerEnter(Collider other)
        {

        }
        public virtual void TriggerStay(Collider other)
        {
        }

        public virtual void TriggerExit(Collider other)
        {

        }
    }
}