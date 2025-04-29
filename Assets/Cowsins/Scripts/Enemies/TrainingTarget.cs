using UnityEngine;
namespace cowsins
{
    public class TrainingTarget : EnemyHealth
    {
        [SerializeField] private float timeToRevive;

        private Animator animator;

        public override void Start()
        {
            animator = GetComponent<Animator>(); 
            base.Start();
        }

        public override void Damage(float damage, bool isHeadshot)
        {
            if (isDead) return;
            animator.Play("Target_Hit");
            base.Damage(damage, isHeadshot);
        }
        public override void Die()
        {
            if (isDead) return;
            isDead = true;
            events.OnDeath?.Invoke();
            Invoke("Revive", timeToRevive);

            if (shieldSlider != null) shieldSlider.gameObject.SetActive(false);
            if (healthSlider != null) healthSlider.gameObject.SetActive(false);

            if (showKillFeed) UIEvents.onEnemyKilled.Invoke(_name);

            if (transform.parent.GetComponent<CompassElement>() != null) transform.parent.GetComponent<CompassElement>().Remove();

            animator.Play("Target_Die");

            SoundManager.Instance.PlaySound(dieSFX, 0, 0, false, 0);
        }
        private void Revive()
        {
            isDead = false;
            animator.Play("Target_Revive");
            health = maxHealth;
            shield = maxShield;

            if (shieldSlider != null) shieldSlider.gameObject.SetActive(true);
            if (healthSlider != null) healthSlider.gameObject.SetActive(true);

            if (transform.parent.GetComponent<CompassElement>() != null) transform.parent.GetComponent<CompassElement>().Add();

#if SAVE_LOAD_ADD_ON
            SaveInteraction();
#endif
        }

#if SAVE_LOAD_ADD_ON
        public override void InteractedState()
        {
            if (health <= 0)
                Revive();
        }
#endif
    }
}