using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class SubtitleDropdown : MonoBehaviour
{
    [SerializeField] private TMP_Dropdown dropdown;

    private void Reset()
    {
        if (dropdown == null)
            dropdown = GetComponent<TMP_Dropdown>();
    }

    private void Start()
    {
        if (dropdown == null)
        {
            Debug.LogError("SubtitleDropdown: TMP_Dropdown belum di-assign!");
            return;
        }

        // Populate dropdown options
        dropdown.ClearOptions();
        dropdown.AddOptions(new List<string>
        {
            "On",  // index 0 = enabled
            "Off"  // index 1 = disabled
        });

        // Sync with saved subtitle setting
        if (SubtitleManager.Instance != null)
        {
            dropdown.value = SubtitleManager.SubtitleEnabled ? 0 : 1;
        }
        else
        {
            // Default to enabled if no SubtitleManager yet
            dropdown.value = 0;
        }

        dropdown.RefreshShownValue();

        // Add listener for value changes
        dropdown.onValueChanged.AddListener(OnDropdownValueChanged);
    }

    private void OnDestroy()
    {
        if (dropdown != null)
        {
            dropdown.onValueChanged.RemoveListener(OnDropdownValueChanged);
        }
    }

    private void OnDropdownValueChanged(int index)
    {
        if (SubtitleManager.Instance != null)
        {
            // index 0 = On (enabled), index 1 = Off (disabled)
            SubtitleManager.Instance.SetSubtitleFromIndex(index);
        }
        else
        {
            Debug.LogWarning("SubtitleDropdown: SubtitleManager belum ada di scene.");
        }
    }
}
