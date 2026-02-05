using UnityEngine;

public class MenuManager : MonoBehaviour
{
    [SerializeField] AudioClip clickSFX;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    [Header("Menu References")]
    [SerializeField] private UnityEngine.UI.Button continueButton;
    [SerializeField] private string firstLevelSceneName = "Gameplay"; // Default ke "Gameplay" sesuai request user

    void Start()
    {
        CheckSavedGame();
    }

    private void CheckSavedGame()
    {
        // Cek apakah ada save game
        if (continueButton != null)
        {
            string savedScene = SaveManager.instance.GetSavedScene();
            // Enable button jika ada saved scene ATAU ada file save (fallback)
            bool hasSave = !string.IsNullOrEmpty(savedScene) || System.IO.File.Exists(System.IO.Path.Combine(Application.persistentDataPath, "playerInfo.dat"));
            continueButton.interactable = hasSave;
        }
    }

    public void NewGame()
    {
        // Reset data lama
        SaveManager.instance.HardResetRunData();
        SaveManager.instance.resetOnStart = true; // Flag untuk reset posisi di PlayerSaveController

        // Load level default
        UnityEngine.SceneManagement.SceneManager.LoadScene(firstLevelSceneName);
    }

    public void ContinueGame()
    {
        SaveManager.instance.resetOnStart = false; // Pastikan tidak di-reset agar posisi ter-load

        string savedScene = SaveManager.instance.GetSavedScene();
        if (string.IsNullOrEmpty(savedScene)) savedScene = firstLevelSceneName; // Fallback ke default jika nama scene kosong

        UnityEngine.SceneManagement.SceneManager.LoadScene(savedScene);
    }

    public void BackToMainMenu()
    {
        // Karena user hanya punya 1 scene "Gameplay", mungkin ini hanya reload scene atau tidak digunakan.
        // Kita set ke "Gameplay" atau biarkan jika user nanti punya MainMenu terpisah.
        UnityEngine.SceneManagement.SceneManager.LoadScene(firstLevelSceneName);
    }

    public void QuitGame()
    {
        Application.Quit();
        Debug.Log("Quit Game");
    }

    public void ResetGameData()
    {
        if (SaveManager.instance != null)
        {
            SaveManager.instance.HardResetRunData();
            CheckSavedGame(); // Update UI (Continue button should be disabled)
            Debug.Log("Game Data Reset via MenuManager");
        }
    }
}
