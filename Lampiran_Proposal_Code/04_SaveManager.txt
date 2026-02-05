using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

public class SaveManager : MonoBehaviour
{
    public static SaveManager instance { get; private set; }

    [Header("Timer")]
    public float timerSeconds;        // akumulasi detik
    public string timerString = "00:00";

    [Header("Quiz Counters")]
    public int totalRight;
    public int totalWrong;

    [Header("Auto Reset On Play")]
    [Tooltip("Jika true, mereset data (timer/score) saat app mulai play.")]
    public bool resetOnStart = false;

    // Registry for lookup
    public System.Collections.Generic.List<cowsins.Weapon_SO> allWeapons = new System.Collections.Generic.List<cowsins.Weapon_SO>();

    private string SavePath => Path.Combine(Application.persistentDataPath, "playerInfo.dat");

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);

        Load();

        if (resetOnStart)
        {
            HardResetRunData();
            // Save(); // JANGAN Save di sini, agar file tidak terbuat sebelum ada checkpoint
        }
    }

    // ====== API UTAMA ======

    public static string FormatTime(float seconds)
    {
        seconds = Mathf.Max(0f, seconds);
        int m = Mathf.FloorToInt(seconds / 60f);
        int s = Mathf.FloorToInt(seconds - m * 60);
        return string.Format("{0:0}:{1:00}", m, s);
    }

    public void AddRight(int amount = 1)
    {
        totalRight += Mathf.Max(0, amount);
    }

    public void AddWrong(int amount = 1)
    {
        totalWrong += Mathf.Max(0, amount);
    }

    public void HardResetRunData()
    {
        timerSeconds = 0f;
        timerString = "00:00";
        totalRight = 0;
        totalWrong = 0;

        // Reset saved position
        PlayerPrefs.DeleteKey("SavedScene");
        if (File.Exists(SavePath)) File.Delete(SavePath);
    }

    // ====== SAVE / LOAD ======

    public void Save()
    {
        // Default save without specific position (keeps current data)
        SaveGame(null, UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }

    public void SaveGame(Vector3? playerPosition = null, string sceneName = "")
    {
        try
        {
            BinaryFormatter bf = new BinaryFormatter();
            using (FileStream file = File.Create(SavePath))
            {
                PlayerData_Storage data = new PlayerData_Storage
                {
                    timerSeconds = timerSeconds,
                    timerString = timerString,
                    totalRight = totalRight,
                    totalWrong = totalWrong,
                    sceneName = string.IsNullOrEmpty(sceneName) ? UnityEngine.SceneManagement.SceneManager.GetActiveScene().name : sceneName
                };

                if (playerPosition.HasValue)
                {
                    data.position = new float[] { playerPosition.Value.x, playerPosition.Value.y, playerPosition.Value.z };
                }

                bf.Serialize(file, data);
            }
        }
        catch (Exception e)
        {
            Debug.LogError("Save failed: " + e.Message);
        }
    }

    public void Load()
    {
        try
        {
            if (File.Exists(SavePath))
            {
                BinaryFormatter bf = new BinaryFormatter();
                using (FileStream file = File.Open(SavePath, FileMode.Open))
                {
                    PlayerData_Storage data = (PlayerData_Storage)bf.Deserialize(file);
                    timerSeconds = data.timerSeconds;
                    timerString = string.IsNullOrEmpty(data.timerString) ? FormatTime(timerSeconds) : data.timerString;
                    totalRight = data.totalRight;
                    totalWrong = data.totalWrong;
                }
            }
            else
            {
                // pertama kali
                timerSeconds = 0f;
                timerString = "00:00";
                totalRight = 0;
                totalWrong = 0;
            }
        }
        catch (Exception e)
        {
            Debug.LogError("Load failed: " + e.Message);
            // fallback aman
            timerSeconds = 0f;
            timerString = "00:00";
            totalRight = 0;
            totalWrong = 0;
        }
    }

    public Vector3? GetSavedPosition()
    {
        try
        {
            if (File.Exists(SavePath))
            {
                BinaryFormatter bf = new BinaryFormatter();
                using (FileStream file = File.Open(SavePath, FileMode.Open))
                {
                    PlayerData_Storage data = (PlayerData_Storage)bf.Deserialize(file);
                    if (data.position != null && data.position.Length == 3)
                    {
                        return new Vector3(data.position[0], data.position[1], data.position[2]);
                    }
                }
            }
        }
        catch { }
        return null;
    }

    public string GetSavedScene()
    {
        try
        {
            if (File.Exists(SavePath))
            {
                BinaryFormatter bf = new BinaryFormatter();
                using (FileStream file = File.Open(SavePath, FileMode.Open))
                {
                    PlayerData_Storage data = (PlayerData_Storage)bf.Deserialize(file);
                    return data.sceneName;
                }
            }
        }
        catch { }
        return null;
    }

    [Serializable]
    public class WeaponData
    {
        public int currentWeaponIndex;
        public System.Collections.Generic.List<WeaponState> weaponStates = new System.Collections.Generic.List<WeaponState>();
    }

    [Serializable]
    public class WeaponState
    {
        public string weaponName; // Added for persistence
        public int bulletsLeftInMagazine;
        public int totalBullets;
    }

    [Serializable]
    class PlayerData_Storage
    {
        // Old fields, kept for compatibility
        public float timerSeconds;
        public string timerString;
        public int totalRight;
        public int totalWrong;

        // New fields
        // Scores
        public int playerMoney;
        public int totalCoinsValue;

        // Timer
        public float puzzleTimer;

        // Evaluation
        public int correctAnswers;
        public int incorrectAnswers;
        public int finalScore;

        // Checkpoint
        public float[] position;
        public string sceneName;
        // Weapons
        public WeaponData weaponData;
    }

    public void SaveGame(Vector3? playerPos, string sceneName, cowsins.WeaponController weaponController = null)
    {
        try
        {
            BinaryFormatter bf = new BinaryFormatter();
            using (FileStream file = File.Create(SavePath))
            {
                PlayerData_Storage data = new PlayerData_Storage
                {
                    timerSeconds = timerSeconds,
                    timerString = timerString,
                    totalRight = totalRight,
                    totalWrong = totalWrong,
                    sceneName = string.IsNullOrEmpty(sceneName) ? UnityEngine.SceneManagement.SceneManager.GetActiveScene().name : sceneName
                };

                if (playerPos.HasValue)
                {
                    data.position = new float[] { playerPos.Value.x, playerPos.Value.y, playerPos.Value.z };
                }

                // Save Weapons
                if (weaponController != null)
                {
                    data.weaponData = new WeaponData();
                    data.weaponData.currentWeaponIndex = weaponController.currentWeapon;

                    for (int i = 0; i < weaponController.inventory.Length; i++)
                    {
                        WeaponState state = new WeaponState();
                        if (weaponController.inventory[i] != null)
                        {
                            state.bulletsLeftInMagazine = weaponController.inventory[i].bulletsLeftInMagazine;
                            state.totalBullets = weaponController.inventory[i].totalBullets;

                            if (weaponController.inventory[i].weapon != null)
                            {
                                state.weaponName = weaponController.inventory[i].weapon.name;
                            }
                        }
                        data.weaponData.weaponStates.Add(state);
                    }
                }

                bf.Serialize(file, data);
            }
        }
        catch (Exception e)
        {
            Debug.LogError("Failed to save game: " + e.Message);
        }
    }


    public void LoadWeapons(cowsins.WeaponController weaponController)
    {
        if (!File.Exists(SavePath)) return;

        try
        {
            BinaryFormatter bf = new BinaryFormatter();
            FileStream file = File.Open(SavePath, FileMode.Open);
            PlayerData_Storage data = (PlayerData_Storage)bf.Deserialize(file);
            file.Close();

            if (data.weaponData != null && weaponController != null)
            {
                // Restore Inventory
                for (int i = 0; i < data.weaponData.weaponStates.Count; i++)
                {
                    if (i >= weaponController.inventory.Length) break;

                    WeaponState state = data.weaponData.weaponStates[i];
                    string savedName = state.weaponName;

                    // Identify if we need to instantiate a new weapon
                    bool needToInstantiate = false;
                    cowsins.Weapon_SO weaponToEquip = null;

                    // Check if slot is empty or mismatch
                    if (!string.IsNullOrEmpty(savedName))
                    {
                        if (weaponController.inventory[i] == null || (weaponController.inventory[i].weapon != null && weaponController.inventory[i].weapon.name != savedName))
                        {
                            // Find in registry
                            weaponToEquip = allWeapons.Find(w => w.name == savedName);
                            if (weaponToEquip != null)
                            {
                                needToInstantiate = true;
                            }
                            else
                            {
                                Debug.LogWarning($"[SaveManager] Could not find weapon with name: {savedName} in All Weapons list.");
                            }
                        }
                    }

                    if (needToInstantiate && weaponToEquip != null)
                    {
                        // Instantiate and set ammo
                        weaponController.InstantiateWeapon(weaponToEquip, i, state.bulletsLeftInMagazine, state.totalBullets);
                    }
                    else if (weaponController.inventory[i] != null)
                    {
                        // Just update ammo
                        weaponController.inventory[i].bulletsLeftInMagazine = state.bulletsLeftInMagazine;
                        weaponController.inventory[i].totalBullets = state.totalBullets;
                    }
                }

                if (data.weaponData.currentWeaponIndex < weaponController.inventory.Length)
                {
                    weaponController.currentWeapon = data.weaponData.currentWeaponIndex;
                }

                // Force equip the loaded weapon visually and process logic
                weaponController.SelectWeapon();
            }
        }
        catch (Exception e)
        {
            Debug.LogError("Failed to load weapons: " + e.Message);
        }
    }
}
