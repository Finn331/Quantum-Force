using UnityEngine;
#if INVENTORY_PRO_ADD_ON
using cowsins.Inventory;
#endif
namespace cowsins
{
    public partial class AttachmentPickeable : Pickeable
    {
        [Tooltip("Attachment to be picked up. Notice that attachment identifiers can be shared among attachments in different weapons.")] public AttachmentIdentifier_SO attachmentIdentifier;

        private int attachmentID;

        private Attachment atc;
        public Attachment Atc => atc;

        private void Start()
        {
            // If the pickeable hasnt been dropped, dont keep going
            if (dropped) return;
            GetVisuals();
        }
        public override void Interact(Transform player)
        {
            // Reference to WeaponController
            WeaponController wCon = player.GetComponent<WeaponController>();

            // If the weapon is null or this is not a compatible attachment for the current unholstered weapon, return
            if (wCon.weapon == null || !CheckCompatibleAttachment(wCon))
            {
#if INVENTORY_PRO_ADD_ON
                if (InventoryProManager.instance._GridGenerator.AddItemToInventory(attachmentIdentifier, 1).Item1)
                {
                    interacted = true;
                    SaveInteraction();
                    Destroy(this.gameObject);
                }
#endif
                interacted = false;
                return;
            }

            // If it is compatible, assign a new attachment
            // Afterwards, unholster the current weapon and destroy this pickeable.
            wCon.AssignNewAttachment(atc, attachmentID);

            wCon.UnHolster(wCon.inventory[wCon.currentWeapon].gameObject, true);

            base.Interact(player);

            Destroy(this.gameObject);
        }

        // Get visuals of the attachment when dropping
        public override void Drop(WeaponController wcon, Transform orientation)
        {
            base.Drop(wcon, orientation);
            GetVisuals();
        }
        public void GetVisuals()
        {
            // Get whatever we need to display
            if (attachmentIdentifier == null)
            {
                Debug.LogError("Attachment Identifier not set-up! Please assign a proper attachment identifier to your existing attachments, otherwise the system won´t work properly.");
                return;
            }
            interactText = attachmentIdentifier._name;
            image.sprite = attachmentIdentifier.icon;
            if (attachmentIdentifier.pickUpGraphics == null) return;
            Destroy(graphics.GetChild(0).gameObject);
            Instantiate(attachmentIdentifier.pickUpGraphics, transform.position, Quaternion.identity, graphics);
        }
        public override bool IsForbiddenInteraction(WeaponController weaponController)
        {
            return AddonManager.instance.isInventoryAddonAvailable
                ? false
                : weaponController.weapon != null && !CheckCompatibleAttachment(weaponController) || weaponController.weapon == null;
        }
        public bool CheckCompatibleAttachment(WeaponController weaponController)
        {
            (bool success, Attachment attachment, int atcId) = CowsinsUtilities.CompatibleAttachment(weaponController.weapon, attachmentIdentifier);
            if (success)
            {
                atc = attachment;
                attachmentID = atcId;
            }
            return success;
        }
    }
}