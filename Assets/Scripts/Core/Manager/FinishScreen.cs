using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class FinishScreen : MonoBehaviour
{
    [Header("UI Refs")]
    [SerializeField] private TextMeshProUGUI timePlayedText;
    [SerializeField] private TextMeshProUGUI rightText;
    [SerializeField] private TextMeshProUGUI wrongText;

    [Header("Optional Buttons")]
    [SerializeField] private string mainMenuScene = "MainMenu";
    [SerializeField] private string playAgainScene = "Level_01";

    void Start()
    {
        if (SaveManager.instance == null)
        {
            Debug.LogWarning("SaveManager.instance == null.");
            SetFallbackTexts();
            return;
        }

        // isi UI
        if (timePlayedText != null) timePlayedText.text = SaveManager.instance.timerString;
        if (rightText != null) rightText.text = SaveManager.instance.totalRight.ToString();
        if (wrongText != null) wrongText.text = SaveManager.instance.totalWrong.ToString();
    }

    private void SetFallbackTexts()
    {
        if (timePlayedText != null) timePlayedText.text = "00:00";
        if (rightText != null) rightText.text = "0";
        if (wrongText != null) wrongText.text = "0";
    }

    // optional: hook ke button UI
    public void OnPlayAgain()
    {
        if (SaveManager.instance != null)
        {
            SaveManager.instance.HardResetRunData();
            SaveManager.instance.Save();
        }
        SceneManager.LoadScene(playAgainScene);
    }

    public void OnMainMenu()
    {
        SceneManager.LoadScene(mainMenuScene);
    }
}
