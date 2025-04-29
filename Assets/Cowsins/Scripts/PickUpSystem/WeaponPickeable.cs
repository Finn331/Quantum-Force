using UnityEngine;
using UnityEditor;
#if INVENTORY_PRO_ADD_ON
using cowsins.Inventory;
#endif
namespace cowsins
{
    public partial class WeaponPickeable : Pickeable
    {
        [Tooltip("Which weapon are we grabbing")] public Weapon_SO weapon;

        [HideInInspector] public int currentBullets, totalBullets;

        private AttachmentIdentifier_SO barrel,
            scope,
            stock,
            grip,
            magazine,
            flashlight,
            laser;
        public AttachmentIdentifier_SO Barrel => barrel;
        public AttachmentIdentifier_SO Scope => scope;
        public AttachmentIdentifier_SO Stock => stock;
        public AttachmentIdentifier_SO Grip => grip;
        public AttachmentIdentifier_SO Magazine => magazine;
        public AttachmentIdentifier_SO Flashlight => flashlight;
        public AttachmentIdentifier_SO Laser => laser;

        public override void Awake()
        {
            base.Awake();
            if (dropped) return;
            Initialize();
        }
        public void Initialize()
        {
            if (weapon == null) return;
            GetVisuals();
            CalculateBulletCounts();
        }

        private void CalculateBulletCounts()
        {
            currentBullets = weapon.magazineSize + SetDefaultAttachments();
            totalBullets = weapon.totalMagazines * currentBullets;
        }

        public override void Interact(Transform player)
        {
            base.Interact(player);
            WeaponController weaponController = player.GetComponent<WeaponController>();
            InteractManager interactManager = player.GetComponent<InteractManager>();

            if (interactManager.DuplicateWeaponAddsBullets)
            {
                for (int i = 0; i < weaponController.inventory.Length; i++)
                {
                    if (weaponController.inventory[i] && weaponController.inventory[i].weapon == weapon && weapon.limitedMagazines)
                    {
                        weaponController.id.totalBullets += 10;
                        DestroyAndSave();
                        return;
                    }
                }
            }

            if (!CheckIfInventoryFull(weaponController))
            {
                DestroyAndSave();
                return;
            }

#if INVENTORY_PRO_ADD_ON
            if (InventoryProManager.instance.StoreWeaponsIfHotbarFull &&
                InventoryProManager.instance._GridGenerator.AddItemToInventory(weapon, 1).Item1)
            {
                DestroyAndSave();
                return;
            }
#endif
            SwapWeapons(weaponController);

            interacted = false;
#if SAVE_LOAD_ADD_ON
            SaveInteraction();
#endif
        }

        private void DestroyAndSave()
        {
#if SAVE_LOAD_ADD_ON
            interacted = true;
            SaveInteraction();
#endif
            Destroy(this.gameObject);
        }

        private bool CheckIfInventoryFull(WeaponController weaponController)
        {
            for (int i = 0; i < weaponController.inventorySize; i++)
            {
                if (weaponController.inventory[i] == null) // Inventory has room for a new weapon.
                {
                    AddWeaponToInventory(weaponController, i);
                    return false;
                }
            }
            // Inventory is full
            return true;
        }

        private void AddWeaponToInventory(WeaponController weaponController, int slot)
        {
            var weaponPicked = Instantiate(weapon.weaponObject, weaponController.weaponHolder);
            weaponPicked.transform.localPosition = weapon.weaponObject.transform.localPosition;

            weaponController.inventory[slot] = weaponPicked;

            if (weaponController.currentWeapon == slot)
            {
                weaponController.weapon = weapon;
                ApplyAttachments(weaponController);
                weaponController.UnHolster(weaponPicked.gameObject, true);
                weaponPicked.gameObject.SetActive(true);
            }
            else
            {
                weaponPicked.gameObject.SetActive(false);
            }

            UpdateWeaponBullets(weaponController.inventory[slot].GetComponent<WeaponIdentification>());

            UpdateWeaponUI(weaponController, slot);

#if UNITY_EDITOR
            UpdateCrosshair(weaponController);
#endif
        }

        private void SwapWeapons(WeaponController weaponController)
        {
            Weapon_SO oldWeapon = weaponController.weapon;
            int savedBulletsLeftInMagazine = weaponController.id.bulletsLeftInMagazine;
            int savedTotalBullets = weaponController.id.totalBullets;
            weaponController.ReleaseCurrentWeapon();

            AddWeaponToInventory(weaponController, weaponController.currentWeapon);

            weaponController.weapon = weapon;

            UpdateWeaponBullets(weaponController.inventory[weaponController.currentWeapon].GetComponent<WeaponIdentification>());

            UpdateWeaponUI(weaponController, weaponController.currentWeapon);

            currentBullets = savedBulletsLeftInMagazine;
            totalBullets = savedTotalBullets;

            weapon = oldWeapon;
            DestroyGraphics();
            GetVisuals();
        }

        private void UpdateWeaponBullets(WeaponIdentification weaponIdentification)
        {
            weaponIdentification.bulletsLeftInMagazine = currentBullets;
            weaponIdentification.totalBullets = totalBullets;
        }

        private void UpdateWeaponUI(WeaponController weaponController, int slot)
        {
            weaponController.weaponsInventoryUISlots[slot].SetWeapon(weapon);
        }

#if UNITY_EDITOR
        private void UpdateCrosshair(WeaponController weaponController)
        {
            if (weaponController.weapon != null)
            {
                var crosshairShape = UIController.instance.crosshair.GetComponent<CrosshairShape>();
                crosshairShape.currentPreset = weaponController.weapon.crosshairPreset;
                CowsinsUtilities.ApplyPreset(crosshairShape.currentPreset, crosshairShape);
            }
        }
#endif


        public override void Drop(WeaponController wcon, Transform orientation)
        {
            base.Drop(wcon, orientation);

            currentBullets = wcon.id.bulletsLeftInMagazine;
            totalBullets = wcon.id.totalBullets;
            weapon = wcon.weapon;
            GetVisuals();
        }

        // Applied the default attachments to the weapon
        private int SetDefaultAttachments()
        {
            DefaultAttachment defaultAttachments = weapon.weaponObject.defaultAttachments;
            barrel = defaultAttachments.defaultBarrel?.attachmentIdentifier;
            scope = defaultAttachments.defaultScope?.attachmentIdentifier;
            stock = defaultAttachments.defaultStock?.attachmentIdentifier;
            grip = defaultAttachments.defaultGrip?.attachmentIdentifier;
            flashlight = defaultAttachments.defaultFlashlight?.attachmentIdentifier;
            this.magazine = defaultAttachments.defaultMagazine?.attachmentIdentifier;
            laser = defaultAttachments.defaultLaser?.attachmentIdentifier;

            if (defaultAttachments.defaultMagazine is Magazine magazine)
            {
                return magazine.magazineCapacityAdded;
            }

            return 0;
        }
        /// <summary>
        /// Stores the attachments on the WeaponPickeable so they can be accessed later in case the weapon is picked up.
        /// </summary>
        public void SetPickeableAttachments(AttachmentIdentifier_SO b, AttachmentIdentifier_SO sc, AttachmentIdentifier_SO st, AttachmentIdentifier_SO gr,
        AttachmentIdentifier_SO mag, AttachmentIdentifier_SO fl, AttachmentIdentifier_SO ls)
        {
            barrel = b;
            scope = sc;
            stock = st;
            grip = gr;
            magazine = mag;
            flashlight = fl;
            laser = ls;
        }
        public void GetVisuals()
        {
            // Get whatever we need to display
            interactText = weapon._name;
            image.sprite = weapon.icon;
            // Manage graphics
            Destroy(graphics.transform.GetChild(0).gameObject);
            Instantiate(weapon.pickUpGraphics, graphics);
        }

        // Equips all the appropriate attachyments on pick up
        public void ApplyAttachments(WeaponController weaponController)
        {
            WeaponIdentification wp = weaponController.inventory[weaponController.currentWeapon];

            var attachments = new[] { barrel, scope, stock, grip, magazine, flashlight, laser };
            foreach (var attachment in attachments)
            {
                (Attachment atc, int id) = CowsinsUtilities.GetAttachmentID(attachment, wp);
                weaponController.AssignNewAttachment(atc, id);
            }
        }
    }

#if UNITY_EDITOR

    [System.Serializable]
    [CustomEditor(typeof(WeaponPickeable))]
    public class WeaponPickeableEditor : Editor
    {
        private string[] tabs = { "Basic", "References", "Effects", "Events" };
        private int currentTab = 0;

        override public void OnInspectorGUI()
        {
            serializedObject.Update();
            WeaponPickeable myScript = target as WeaponPickeable;

            Texture2D myTexture = Resources.Load<Texture2D>("CustomEditor/WeaponPickeable_CustomEditor") as Texture2D;
            GUILayout.Label(myTexture);

            EditorGUILayout.BeginVertical();
            currentTab = GUILayout.Toolbar(currentTab, tabs);
            EditorGUILayout.Space(10f);
            EditorGUILayout.EndVertical();
            #region variables

            if (currentTab >= 0 || currentTab < tabs.Length)
            {
                switch (tabs[currentTab])
                {
                    case "Basic":
                        EditorGUILayout.LabelField("CUSTOMIZE YOUR WEAPON PICKEABLE", EditorStyles.boldLabel);
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("weapon"));
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("interactText"));
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("instantInteraction"));
                        break;
                    case "References":
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("image"));
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("graphics"));

                        break;
                    case "Effects":
                        EditorGUILayout.LabelField("EFFECTS", EditorStyles.boldLabel);
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("rotates"));
                        if (myScript.rotates) EditorGUILayout.PropertyField(serializedObject.FindProperty("rotationSpeed"));

                        EditorGUILayout.PropertyField(serializedObject.FindProperty("translates"));
                        if (myScript.translates) EditorGUILayout.PropertyField(serializedObject.FindProperty("translationSpeed"));
                        break;
                    case "Events":
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("events"));
                        break;

                }
            }

            #endregion

            serializedObject.ApplyModifiedProperties();

        }
    }
#endif
}