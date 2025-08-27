using cowsins;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelManager : MonoBehaviour
{
    [Header("Score (runtime)")]
    [SerializeField] int rightScore;
    [SerializeField] int wrongScore;

    [Header("Runtime UI (optional)")]
    [SerializeField] TextMeshProUGUI rightScoreText;
    [SerializeField] TextMeshProUGUI wrongScoreText;
    [SerializeField] TextMeshProUGUI timerText;

    [Header("Finish Panel (animated)")]
    [Tooltip("Root RectTransform dari Finish Menu Container")]
    [SerializeField] RectTransform finishPanelRoot;
    [Tooltip("CanvasGroup di root untuk fade in/out & block raycast")]
    [SerializeField] CanvasGroup finishPanelCG;

    [Header("Finish Panel Sub-Groups (urut anim)")]
    [SerializeField] RectTransform headerGroup;   // Header Panel / Header Text
    [SerializeField] RectTransform timeGroup;     // Time
    [SerializeField] RectTransform rightGroup;    // Total Right
    [SerializeField] RectTransform wrongGroup;    // Total Wrong
    [SerializeField] RectTransform buttonGroup;   // Button (Main Menu)

    [Header("Finish Panel Texts")]
    [SerializeField] TextMeshProUGUI finishTimerText;
    [SerializeField] TextMeshProUGUI finishRightText;
    [SerializeField] TextMeshProUGUI finishWrongText;

    [Header("SFX")]
    [SerializeField] AudioSource audioSource;
    [SerializeField] AudioClip openSfx;
    [SerializeField, Range(0f, 1f)] float openSfxVolume = 1f;

    [Header("Player Scripts")]
    [SerializeField] PlayerMovement playerMovement;
    [SerializeField] WeaponController weaponController;

    [Header("Scene")]
    [SerializeField] string mainMenuScene = "MainMenu";

    // guard supaya tidak buka 2x
    private bool finishOpened = false;

    // ----------------- LIFECYCLE -----------------
    void Start()
    {
        // Init score dari SaveManager bila ada
        if (SaveManager.instance != null)
        {
            rightScore = SaveManager.instance.totalRight;
            wrongScore = SaveManager.instance.totalWrong;
            if (timerText != null) timerText.text = SaveManager.instance.timerString;
        }

        UpdateRuntimeUI();

        // PASTIKAN PANEL TERTUTUP DI AWAL
        ForceCloseFinishPanel();   // <- ini yang menutup & menyiapkan posisi awal
    }

    void Update()
    {
        if (SaveManager.instance == null) return;

        // jalanin timer runtime
        SaveManager.instance.timerSeconds += Time.deltaTime;
        SaveManager.instance.timerString = SaveManager.FormatTime(SaveManager.instance.timerSeconds);

        if (timerText != null) timerText.text = SaveManager.instance.timerString;
    }

    // ----------------- SCORE API -----------------
    public void RightScoreAdd()
    {
        rightScore++;
        if (SaveManager.instance != null) SaveManager.instance.AddRight(1);
        UpdateRuntimeUI();
    }

    public void WrongScoreAdd()
    {
        wrongScore++;
        if (SaveManager.instance != null) SaveManager.instance.AddWrong(1);
        UpdateRuntimeUI();
    }

    void UpdateRuntimeUI()
    {
        if (rightScoreText != null) rightScoreText.text = rightScore.ToString("00");
        if (wrongScoreText != null) wrongScoreText.text = wrongScore.ToString("00");
        if (timerText != null && SaveManager.instance != null) timerText.text = SaveManager.instance.timerString;
    }

    // ----------------- FINISH (TRIGGER DARI GAME) -----------------
    // Panggil method ini ketika level selesai (via trigger, event, dsb)
    public void OpenFinishFromTrigger()
    {
        if (!finishOpened) FinishLevel();
    }

    public void FinishLevel()
    {
        if (finishOpened) return;      // prevention buka 2x
        finishOpened = true;

        UnlockCursorAndDisablePlayer();

        if (SaveManager.instance != null) SaveManager.instance.Save();

        // inject nilai ke finish text
        if (finishTimerText != null) finishTimerText.text = SaveManager.instance != null ? SaveManager.instance.timerString : "00:00";
        if (finishRightText != null) finishRightText.text = rightScore.ToString("00");
        if (finishWrongText != null) finishWrongText.text = wrongScore.ToString("00");

        PlayFinishPanelOpen();
    }

    void UnlockCursorAndDisablePlayer()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        if (playerMovement != null) playerMovement.enabled = false;
        if (weaponController != null) weaponController.enabled = false;
    }

    // ----------------- ANIMATION -----------------
    // Tutup panel secara paksa di awal game (benar2 tak terlihat & tak interaktif)
    void ForceCloseFinishPanel()
    {
        if (finishPanelRoot == null) return;

        LeanTween.cancel(finishPanelRoot.gameObject);

        // NONAKTIFKAN supaya tidak mengganggu UI lain
        finishPanelRoot.gameObject.SetActive(false);

        // siapkan state awal (akan dipakai saat open)
        if (finishPanelCG != null)
        {
            finishPanelCG.alpha = 0f;
            finishPanelCG.blocksRaycasts = false;
            finishPanelCG.interactable = false;
        }

        // taruh sub group sedikit di bawah utk efek slide saat nanti dibuka
        float offsetY = -60f;
        SetStartY(headerGroup, offsetY);
        SetStartY(timeGroup, offsetY);
        SetStartY(rightGroup, offsetY);
        SetStartY(wrongGroup, offsetY);
        SetStartY(buttonGroup, offsetY);

        // scale ke 0 agar pop-in mulus ketika diaktifkan
        finishPanelRoot.localScale = Vector3.zero;
    }

    void PlayFinishPanelOpen()
    {
        if (finishPanelRoot == null) return;

        // aktifkan dulu baru tween
        finishPanelRoot.gameObject.SetActive(true);

        if (audioSource != null && openSfx != null) audioSource.PlayOneShot(openSfx, openSfxVolume);

        LeanTween.cancel(finishPanelRoot.gameObject);
        finishPanelRoot.localScale = Vector3.zero;
        if (finishPanelCG != null) finishPanelCG.alpha = 0f;

        // Pop-in
        LeanTween.scale(finishPanelRoot, Vector3.one, 0.55f).setEase(LeanTweenType.easeOutBack);

        if (finishPanelCG != null)
        {
            LeanTween.value(finishPanelRoot.gameObject, 0f, 1f, 0.35f)
                     .setOnUpdate(a => finishPanelCG.alpha = a)
                     .setOnComplete(() =>
                     {
                         // setelah terlihat, baru boleh tangkap input
                         finishPanelCG.blocksRaycasts = true;
                         finishPanelCG.interactable = true;
                     });
        }

        // slide-in berurutan
        float d = 0.08f;
        SlideUp(headerGroup, 0f);
        SlideUp(timeGroup, d * 1);
        SlideUp(rightGroup, d * 2);
        SlideUp(wrongGroup, d * 3);
        SlideUp(buttonGroup, d * 4);
    }

    void SetStartY(RectTransform rt, float startYOffset)
    {
        if (rt == null) return;
        var p = rt.anchoredPosition;
        p.y = startYOffset;
        rt.anchoredPosition = p;

        var cg = rt.GetComponent<CanvasGroup>();
        if (cg != null) cg.alpha = 0f;
    }

    void SlideUp(RectTransform rt, float delay)
    {
        if (rt == null) return;
        var cg = rt.GetComponent<CanvasGroup>();
        if (cg != null) cg.alpha = 0f;

        LeanTween.moveY(rt, 0f, 0.35f)
                 .setEase(LeanTweenType.easeOutCubic)
                 .setDelay(delay);

        if (cg != null)
        {
            LeanTween.value(rt.gameObject, 0f, 1f, 0.28f)
                     .setDelay(delay + 0.05f)
                     .setOnUpdate(a => cg.alpha = a);
        }
    }

    // ----------------- BUTTON HOOKS -----------------
    public void Button_MainMenu()
    {
        if (SaveManager.instance != null) SaveManager.instance.Save();
        SceneManager.LoadScene(mainMenuScene);
    }
}
