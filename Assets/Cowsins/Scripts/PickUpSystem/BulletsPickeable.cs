using UnityEngine;
#if INVENTORY_PRO_ADD_ON
using cowsins.Inventory;
#endif
namespace cowsins
{
    public partial class BulletsPickeable : Pickeable
    {
        [Tooltip("How many bullets you will get"), SerializeField] private int amountOfBullets;

        [SerializeField] private BulletsItem_SO bulletsSO;

        [SerializeField] private Sprite bulletsIcon;

        [SerializeField] private GameObject bulletsGraphics;

        public int AmountOfBullets => amountOfBullets;

        public override void Awake()
        {
            base.Awake();
            image.sprite = bulletsIcon;
            Destroy(graphics.transform.GetChild(0).gameObject);
            Instantiate(bulletsGraphics, graphics);
        }
        public override void Interact(Transform player)
        {
            #if INVENTORY_PRO_ADD_ON
            if (InventoryProManager.instance._GridGenerator.AddItemToInventory(bulletsSO, amountOfBullets).Item1)
            {
                interacted = true;
                interactableEvents.OnInteract?.Invoke();
                SaveInteraction();
                Destroy(this.gameObject);
                return;
            }
            #else
            if (player.GetComponent<WeaponController>().weapon == null) return;
            #endif
            interacted = true; 
            base.Interact(player);
            player.GetComponent<WeaponController>().id.totalBullets += amountOfBullets;
            Destroy(this.gameObject);
        }
        public void SetBullets(BulletsItem_SO bulletsSO, int amountOfBullets)
        {
            this.amountOfBullets = amountOfBullets;
            this.bulletsSO = bulletsSO;
        }

        public override bool IsForbiddenInteraction(WeaponController weaponController)
        {
            return AddonManager.instance.isInventoryAddonAvailable
                ? false
                : weaponController.weapon != null && !weaponController.weapon.limitedMagazines || weaponController.weapon == null;
        }
    }
}