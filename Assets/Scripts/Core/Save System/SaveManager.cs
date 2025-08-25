using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

public class SaveManager : MonoBehaviour
{
    public static SaveManager instance { get; private set; }

    [Header("Timer")]
    public float timerSeconds;     // sumber kebenaran
    public string timerString;     // format tampilan

    [Header("Quiz")]
    public int totalWrong;
    public int totalRight;

    private string SavePath => Application.persistentDataPath + "/playerInfo.dat";

    private void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;
        DontDestroyOnLoad(gameObject);
        Load();
        // sinkronkan string dari seconds (jaga-jaga file lama tak punya string)
        timerString = FormatTime(timerSeconds);
    }

    public void Load()
    {
        if (!File.Exists(SavePath)) return;

        var bf = new BinaryFormatter();
        using (var file = File.Open(SavePath, FileMode.Open))
        {
            PlayerData_Storage data = (PlayerData_Storage)bf.Deserialize(file);
            timerSeconds = data.timerSeconds;
            timerString = data.timerString;
            totalWrong = data.totalWrong;
            totalRight = data.totalRight;
        }
    }

    public void Save()
    {
        // pastikan string sesuai seconds
        timerString = FormatTime(timerSeconds);

        var bf = new BinaryFormatter();
        using (var file = File.Create(SavePath))
        {
            var data = new PlayerData_Storage
            {
                timerSeconds = timerSeconds,
                timerString = timerString,
                totalWrong = totalWrong,
                totalRight = totalRight
            };
            bf.Serialize(file, data);
        }
#if UNITY_EDITOR
        Debug.Log($"[SaveManager] Saved to: {SavePath} ({timerString}, R:{totalRight} W:{totalWrong})");
#endif
    }

    public static string FormatTime(float seconds)
    {
        int minutes = Mathf.FloorToInt(seconds / 60f);
        int secs = Mathf.FloorToInt(seconds - minutes * 60);
        return $"{minutes:0}:{secs:00}";
    }
}

[Serializable]
class PlayerData_Storage
{
    public float timerSeconds;
    public string timerString;
    public int totalWrong;
    public int totalRight;
}
