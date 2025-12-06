using UnityEngine;
using System;

public enum GameLanguage
{
    Indonesian = 0,
    English = 1
}

public class LanguageManager : MonoBehaviour
{
    public static LanguageManager Instance { get; private set; }

    public static GameLanguage CurrentLanguage { get; private set; } = GameLanguage.Indonesian;

    public static event Action<GameLanguage> OnLanguageChanged;

    private const string PlayerPrefsKey = "GameLanguage";

    private void Awake()
    {
        // Singleton
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadLanguage();
    }

    private void LoadLanguage()
    {
        int langIndex = PlayerPrefs.GetInt(PlayerPrefsKey, 0); // default Indonesian
        CurrentLanguage = (GameLanguage)langIndex;
    }

    public void SetLanguage(GameLanguage language)
    {
        if (CurrentLanguage == language) return;

        CurrentLanguage = language;
        PlayerPrefs.SetInt(PlayerPrefsKey, (int)language);
        PlayerPrefs.Save();

        OnLanguageChanged?.Invoke(CurrentLanguage);
    }

    // Dipanggil dari Dropdown (parameter int index)
    public void SetLanguageFromIndex(int index)
    {
        SetLanguage((GameLanguage)index);
    }
}
