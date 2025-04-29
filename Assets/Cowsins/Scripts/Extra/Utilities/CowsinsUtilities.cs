using System;
using UnityEngine;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor.Presets;
using UnityEditor;
#endif
using System.IO;

namespace cowsins
{
    public static class CowsinsUtilities
    {
        /// <summary>
        /// Returns a Vector3 that applies spread to the bullets shot
        /// </summary>
        public static Vector3 GetSpreadDirection(float amount, Camera camera)
        {
            float horSpread = UnityEngine.Random.Range(-amount, amount);
            float verSpread = UnityEngine.Random.Range(-amount, amount);
            Vector3 spread = camera.transform.InverseTransformDirection(new Vector3(horSpread, verSpread, 0));
            Vector3 dir = camera.transform.forward + spread;

            return dir;
        }
        public static void PlayAnim(string anim, Animator animator)
        {
            animator.SetTrigger(anim);
        }

        public static void ForcePlayAnim(string anim, Animator animator)
        {
            animator.Play(anim, 0, 0);
        }
        public static void StartAnim(string anim, Animator animated) => animated.SetBool(anim, true);

        public static void StopAnim(string anim, Animator animated) => animated.SetBool(anim, false);
#if UNITY_EDITOR
        public static void SavePreset(UnityEngine.Object source, string name)
        {
            if (EmptyString(name))
            {
                Debug.LogError("ERROR: Do not forget to give your preset a name!");
                return;
            }
            Preset preset = new Preset(source);

            string directoryPath = "Assets/" + "Cowsins/" + "CowsinsPresets/";

            if (!Directory.Exists(directoryPath)) Directory.CreateDirectory(directoryPath);

            AssetDatabase.CreateAsset(preset, directoryPath + name + ".preset");
            Debug.Log("Preset successfully saved");
        }
        public static void ApplyPreset(Preset preset, UnityEngine.Object target)
        {
            preset.ApplyTo(target);
        }

        public static bool IsUsingUnity6()
        {
            string unityVersion = Application.unityVersion;
            return unityVersion.StartsWith("6"); 
        }

#endif
        public static bool EmptyString(string string_)
        {
            if (string_.Length == 0) return true;
            int i = 0;
            while (i < string_.Length)
            {
                if (string_[i].ToString() == " ") return true;
                i++;
            }
            return false;
        }

        public static IDamageable GatherDamageableParent(Transform child)
        {
            for (Transform parent = child.parent; parent != null; parent = parent.parent)
            {
                if (parent.TryGetComponent(out IDamageable component))
                {
                    return component;
                }
            }
            return null;
        }


        /// <summary>
        /// Grabs the attachment object and the id given an attachment identifier
        /// </summary>
        /// <param name="atcToCheck">Attachment object to get information about returned.</param>
        /// <param name="wID">Weapon Identification that holds the attachments</param>
        /// <returns></returns>
        public static (Attachment, int) GetAttachmentID(AttachmentIdentifier_SO atcToCheck, WeaponIdentification wID)
        {
            // Check for compatibility
            for (int i = 0; i < wID.compatibleAttachments.barrels.Length; i++)
            {
                if (atcToCheck == wID.compatibleAttachments.barrels[i].attachmentIdentifier) return (wID.compatibleAttachments.barrels[i], i);
            }
            for (int i = 0; i < wID.compatibleAttachments.scopes.Length; i++)
            {
                if (atcToCheck == wID.compatibleAttachments.scopes[i].attachmentIdentifier) return (wID.compatibleAttachments.scopes[i], i);
            }
            for (int i = 0; i < wID.compatibleAttachments.stocks.Length; i++)
            {
                if (atcToCheck == wID.compatibleAttachments.stocks[i].attachmentIdentifier) return (wID.compatibleAttachments.stocks[i], i);
            }
            for (int i = 0; i < wID.compatibleAttachments.grips.Length; i++)
            {
                if (atcToCheck == wID.compatibleAttachments.grips[i].attachmentIdentifier) return (wID.compatibleAttachments.grips[i], i);
            }
            for (int i = 0; i < wID.compatibleAttachments.magazines.Length; i++)
            {
                if (atcToCheck == wID.compatibleAttachments.magazines[i].attachmentIdentifier) return (wID.compatibleAttachments.magazines[i], i);
            }
            for (int i = 0; i < wID.compatibleAttachments.flashlights.Length; i++)
            {
                if (atcToCheck == wID.compatibleAttachments.flashlights[i].attachmentIdentifier) return (wID.compatibleAttachments.flashlights[i], i);
            }
            for (int i = 0; i < wID.compatibleAttachments.lasers.Length; i++)
            {
                if (atcToCheck == wID.compatibleAttachments.lasers[i].attachmentIdentifier) return (wID.compatibleAttachments.lasers[i], i);
            }

            // Return an error
            return (null, -1);
        }

        /// <summary>
        /// Checks if the attachment is compatible with the current unholstered weapon
        /// </summary>
        /// <param name="weapon">Weapon to check compatibility</param>
        /// <returns></returns>
        public static (bool, Attachment, int) CompatibleAttachment(Weapon_SO weapon, AttachmentIdentifier_SO attachmentIdentifier)
        {
            if (weapon?.weaponObject == null) return (false, null, -1);

            // Loop through all the different compatible attachments types
            for (int i = 0; i < weapon.weaponObject.compatibleAttachments.barrels.Length; i++)
            {
                // If the "i" element of the compatible attachments array selected is equal to this attachment, assign it.
                if (weapon.weaponObject.compatibleAttachments.barrels[i].attachmentIdentifier == attachmentIdentifier)
                    return (true, weapon.weaponObject.compatibleAttachments.barrels[i], i);
            }
            for (int i = 0; i < weapon.weaponObject.compatibleAttachments.scopes.Length; i++)
            {
                if (weapon.weaponObject.compatibleAttachments.scopes[i].attachmentIdentifier == attachmentIdentifier)
                    return (true, weapon.weaponObject.compatibleAttachments.scopes[i], i);
            }
            for (int i = 0; i < weapon.weaponObject.compatibleAttachments.stocks.Length; i++)
            {
                if (weapon.weaponObject.compatibleAttachments.stocks[i].attachmentIdentifier == attachmentIdentifier)
                    return (true, weapon.weaponObject.compatibleAttachments.stocks[i], i);
            }
            for (int i = 0; i < weapon.weaponObject.compatibleAttachments.grips.Length; i++)
            {
                if (weapon.weaponObject.compatibleAttachments.grips[i].attachmentIdentifier == attachmentIdentifier)
                    return (true, weapon.weaponObject.compatibleAttachments.grips[i], i);
            }
            for (int i = 0; i < weapon.weaponObject.compatibleAttachments.magazines.Length; i++)
            {
                if (weapon.weaponObject.compatibleAttachments.magazines[i].attachmentIdentifier == attachmentIdentifier)
                    return (true, weapon.weaponObject.compatibleAttachments.magazines[i], i);
            }
            for (int i = 0; i < weapon.weaponObject.compatibleAttachments.flashlights.Length; i++)
            {
                if (weapon.weaponObject.compatibleAttachments.flashlights[i].attachmentIdentifier == attachmentIdentifier)
                    return (true, weapon.weaponObject.compatibleAttachments.flashlights[i], i);
            }
            for (int i = 0; i < weapon.weaponObject.compatibleAttachments.lasers.Length; i++)
            {
                if (weapon.weaponObject.compatibleAttachments.lasers[i].attachmentIdentifier == attachmentIdentifier)
                    return (true, weapon.weaponObject.compatibleAttachments.lasers[i], i);
            }
            return (false, null, -1);
        }


        public static bool IsAddonAvailable(string className)
        {
            return AppDomain.CurrentDomain.GetAssemblies().SelectMany(assembly => assembly.GetTypes()).Any(type => type.FullName == className);
        }
    }
}
