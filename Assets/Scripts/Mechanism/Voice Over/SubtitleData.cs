using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class SubtitleEntry
{
    [Tooltip("Waktu mulai subtitle dalam detik (relatif ke awal audio)")]
    public float startTime;

    [Tooltip("Durasi subtitle ditampilkan (dalam detik)")]
    public float duration = 2f;

    [TextArea(2, 4)]
    [Tooltip("Teks subtitle dalam Bahasa Indonesia")]
    public string indonesianText;

    [TextArea(2, 4)]
    [Tooltip("Subtitle text in English")]
    public string englishText;

    /// <summary>
    /// Get text based on current language setting
    /// </summary>
    public string GetText()
    {
        GameLanguage lang = GameLanguage.Indonesian;
        if (LanguageManager.Instance != null)
        {
            lang = LanguageManager.CurrentLanguage;
        }

        return lang switch
        {
            GameLanguage.Indonesian => !string.IsNullOrEmpty(indonesianText) ? indonesianText : englishText,
            GameLanguage.English => !string.IsNullOrEmpty(englishText) ? englishText : indonesianText,
            _ => indonesianText
        };
    }
}

[CreateAssetMenu(fileName = "NewSubtitleData", menuName = "Quantum Force/Subtitle Data", order = 1)]
public class SubtitleData : ScriptableObject
{
    [Tooltip("List of subtitle entries for this voice over")]
    public List<SubtitleEntry> entries = new List<SubtitleEntry>();

    /// <summary>
    /// Get subtitle entry at specified time
    /// </summary>
    public SubtitleEntry GetEntryAtTime(float time)
    {
        foreach (var entry in entries)
        {
            if (time >= entry.startTime && time < entry.startTime + entry.duration)
            {
                return entry;
            }
        }
        return null;
    }

    /// <summary>
    /// Get all entries sorted by start time
    /// </summary>
    public List<SubtitleEntry> GetSortedEntries()
    {
        var sorted = new List<SubtitleEntry>(entries);
        sorted.Sort((a, b) => a.startTime.CompareTo(b.startTime));
        return sorted;
    }
}
