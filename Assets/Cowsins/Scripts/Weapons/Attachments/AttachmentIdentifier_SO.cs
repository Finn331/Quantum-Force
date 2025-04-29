using UnityEngine;
#if INVENTORY_PRO_ADD_ON
using cowsins.Inventory;
#endif
namespace cowsins
{
    [CreateAssetMenu(fileName = "NewAttachmentIdentifier", menuName = "COWSINS/New Attachment Identifier", order = 2)]
    public class AttachmentIdentifier_SO : Item_SO
    {
#if INVENTORY_PRO_ADD_ON
        public override void Use(InventoryProManager inventoryProManager, InventorySlot slot)
        {
            if (inventoryProManager._WeaponController.id == null) 
            {
                ToastManager.Instance?.ShowToast(ToastManager.Instance.AttachmentNotCompatibleMsg);
                return;
            }

            (bool success, Attachment attachment, int attachmentIdentifier) = CowsinsUtilities.CompatibleAttachment(inventoryProManager._WeaponController, this);
            if (!success)
            {
                ToastManager.Instance?.ShowToast("This attachment is not compatible");
                return;
            }
            inventoryProManager._WeaponController.AssignAttachmentToWeapon(attachment, attachmentIdentifier, inventoryProManager._WeaponController.currentWeapon);
            inventoryProManager._GridGenerator.ClearSlotArea(slot);
        }
#endif
    }
}