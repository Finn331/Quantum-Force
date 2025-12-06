using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class LanguageDropdown : MonoBehaviour
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
            Debug.LogError("LanguageDropdown: TMP_Dropdown belum di-assign!");
            return;
        }

        // Isi opsi dropdown
        dropdown.ClearOptions();
        dropdown.AddOptions(new List<string>
        {
            "Bahasa Indonesia",
            "English"
        });

        // Sinkron dengan bahasa tersimpan
        if (LanguageManager.Instance != null)
        {
            dropdown.value = (int)LanguageManager.CurrentLanguage;
        }

        dropdown.RefreshShownValue();

        // Listener value change
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
        if (LanguageManager.Instance != null)
        {
            LanguageManager.Instance.SetLanguageFromIndex(index);
        }
        else
        {
            Debug.LogWarning("LanguageDropdown: LanguageManager belum ada di scene.");
        }
    }
}
