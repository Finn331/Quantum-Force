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
            Save(); // tulis kondisi baru
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
    }

    // ====== SAVE / LOAD ======

    public void Save()
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
                    totalWrong = totalWrong
                };
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

    [Serializable]
    class PlayerData_Storage
    {
        public float timerSeconds;
        public string timerString;
        public int totalRight;
        public int totalWrong;
    }
}
