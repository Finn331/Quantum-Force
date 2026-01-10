using UnityEngine;
using cowsins;

public class PlayerSaveController : MonoBehaviour
{
    void Start()
    {
        Debug.Log($"[PlayerSaveController] Start. SaveManager Instance: {SaveManager.instance != null}, ResetOnStart: {SaveManager.instance?.resetOnStart}");

        // Don't load if SaveManager is missing or resetOnStart is true
        if (SaveManager.instance == null) return;

        if (SaveManager.instance.resetOnStart)
        {
            Debug.Log("[PlayerSaveController] Skipping load because resetOnStart is TRUE (New Game).");
            // Set resetOnStart kembali ke false agar kalau mati & restart (bukan new game) bisa load checkpoint
            SaveManager.instance.resetOnStart = false;
            return;
        }

        Invoke(nameof(RestoreState), 0.1f);
    }

    void RestoreState()
    {
        Vector3? savedPos = SaveManager.instance.GetSavedPosition();
        Debug.Log($"[PlayerSaveController] Attempting Restore. SavedPos: {savedPos}");

        if (savedPos.HasValue && savedPos.Value != Vector3.zero)
        {
            // Position
            CharacterController cc = GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;

            transform.position = savedPos.Value;
            Debug.Log($"[PlayerSaveController] Player Teleported to {savedPos.Value}");

            if (cc != null) cc.enabled = true;

            // Update respawn position in PlayerStats
            PlayerStats stats = GetComponent<PlayerStats>();
            if (stats != null)
            {
                stats.respawnPosition = savedPos.Value;
            }

            // Restore Weapons
            cowsins.WeaponController wc = GetComponent<cowsins.WeaponController>();
            if (wc != null)
            {
                Debug.Log("[PlayerSaveController] Loading Weapons...");
                SaveManager.instance.LoadWeapons(wc);
            }
        }
        else
        {
            Debug.LogWarning("[PlayerSaveController] Saved position is invalid or missing.");
        }
    }
}
