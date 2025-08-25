using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelManager : MonoBehaviour
{
    [Header("Score Settings")]
    [SerializeField] int rightScore;
    [SerializeField] int wrongScore;

    [Header("Timer Settings")]
    [SerializeField] bool autoSaveEverySecond = false;
    private float autoSaveTimer;

    [Header("Finish Scene")]
    [SerializeField] string finishSceneName = "Finish";

    void Start()
    {
        // pastikan SaveManager sudah ada
        if (SaveManager.instance != null)
        {
            // muat skor yang sudah tersimpan (kalau memang mau melanjutkan)
            rightScore = SaveManager.instance.totalRight;
            wrongScore = SaveManager.instance.totalWrong;
        }
        else
        {
            Debug.LogWarning("SaveManager.instance == null. Pastikan SaveManager ada di scene awal.");
        }
    }

    void Update()
    {
        // hitung timer per frame (bukan FixedUpdate)
        if (SaveManager.instance != null)
        {
            SaveManager.instance.timerSeconds += Time.deltaTime;
            SaveManager.instance.timerString = SaveManager.FormatTime(SaveManager.instance.timerSeconds);
        }

        // optional: autosave tiap 1 detik
        if (autoSaveEverySecond && SaveManager.instance != null)
        {
            autoSaveTimer += Time.deltaTime;
            if (autoSaveTimer >= 1f)
            {
                autoSaveTimer = 0f;
                SaveManager.instance.Save();
            }
        }
    }

    public void RightScoreAdd()
    {
        rightScore++;
        if (SaveManager.instance != null) SaveManager.instance.totalRight = rightScore;
        Debug.Log("Right Score: " + rightScore);
    }

    public void WrongScoreAdd()
    {
        wrongScore++;
        if (SaveManager.instance != null) SaveManager.instance.totalWrong = wrongScore;
        Debug.Log("Wrong Score: " + wrongScore);
    }

    public void FinishLevel()
    {
        Debug.Log("Level Finished!");
        if (SaveManager.instance != null) SaveManager.instance.Save();
        SceneManager.LoadScene(finishSceneName);
    }
}
