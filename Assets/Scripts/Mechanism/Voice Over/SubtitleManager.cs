using UnityEngine;
using System;

public class SubtitleManager : MonoBehaviour
{
    public static SubtitleManager Instance { get; private set; }

    public static bool SubtitleEnabled { get; private set; } = true;

    public static event Action<bool> OnSubtitleSettingChanged;

    private const string PlayerPrefsKey = "SubtitleEnabled";

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

        LoadSettings();
    }

    private void LoadSettings()
    {
        // 1 = enabled, 0 = disabled. Default: enabled (1)
        int savedValue = PlayerPrefs.GetInt(PlayerPrefsKey, 1);
        SubtitleEnabled = savedValue == 1;
    }

    public void SetSubtitleEnabled(bool enabled)
    {
        if (SubtitleEnabled == enabled) return;

        SubtitleEnabled = enabled;
        PlayerPrefs.SetInt(PlayerPrefsKey, enabled ? 1 : 0);
        PlayerPrefs.Save();

        OnSubtitleSettingChanged?.Invoke(SubtitleEnabled);
    }

    // Called from Dropdown (parameter int index: 0 = On, 1 = Off)
    public void SetSubtitleFromIndex(int index)
    {
        SetSubtitleEnabled(index == 0);
    }
}
