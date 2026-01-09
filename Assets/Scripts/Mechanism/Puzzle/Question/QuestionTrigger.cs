using cowsins;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// Data class untuk menyimpan satu pertanyaan quiz
/// </summary>
[System.Serializable]
public class QuizQuestion
{
    [TextArea(2, 4)]
    public string questionText;
    
    public string[] answerOptions = new string[4]; // A, B, C, D
    
    [Tooltip("Index jawaban benar (0 = A, 1 = B, 2 = C, 3 = D)")]
    [Range(0, 3)]
    public int correctAnswerIndex;
}

public class QuestionTrigger : MonoBehaviour
{
    [Header("Question Data")]
    [Tooltip("Daftar soal yang akan ditampilkan. Jika lebih dari 1, akan diacak.")]
    [SerializeField] private QuizQuestion[] questions;

    [Header("UI References")]
    [SerializeField] private GameObject questionPanel;
    [SerializeField] private AudioClip panelOpenSFX;
    [SerializeField] private GameObject rightOrWrongTextPanel;
    [SerializeField] private GameObject rightAnswerText;
    [SerializeField] private GameObject wrongAnswerText;

    [Header("Question Display UI")]
    [Tooltip("Text untuk menampilkan soal")]
    [SerializeField] private TMP_Text questionDisplayText;
    [Tooltip("Button-button jawaban (urutan: A, B, C, D)")]
    [SerializeField] private Button[] answerButtons;
    [Tooltip("Text pada setiap button jawaban (urutan: A, B, C, D)")]
    [SerializeField] private TMP_Text[] answerButtonTexts;

    [Header("Player Reference")]
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private PauseMenu pauseMenu;
    [SerializeField] private GameObject crosshair;

    [Header("Custom Events")]
    [Tooltip("Dipanggil ketika jawaban benar.")]
    [SerializeField] private UnityEvent onRightAnswer;
    [Tooltip("Dipanggil ketika jawaban salah.")]
    [SerializeField] private UnityEvent onWrongAnswer;

    // PlayerMovement Settings
    private float originalWalkSpeed = 5f;
    private float originalRunSpeed = 10f;
    private float originalAcceleration = 4500f;
    private float originalJumpForce = 10f;
    private float cameraSensitivity = 4f;

    private GameObject localGameobject;

    // --- QUIZ STATE ---
    private bool isAnswered = false;
    private QuizQuestion currentQuestion;
    private List<int> availableQuestionIndices = new List<int>();

    private void Start()
    {
        localGameobject = gameObject;
        InitializeQuestionPool();
        SetupAnswerButtonListeners();
    }

    /// <summary>
    /// Inisialisasi pool index soal yang tersedia
    /// </summary>
    private void InitializeQuestionPool()
    {
        availableQuestionIndices.Clear();
        for (int i = 0; i < questions.Length; i++)
        {
            availableQuestionIndices.Add(i);
        }
    }

    /// <summary>
    /// Setup listener untuk setiap button jawaban
    /// </summary>
    private void SetupAnswerButtonListeners()
    {
        if (answerButtons == null) return;

        for (int i = 0; i < answerButtons.Length; i++)
        {
            if (answerButtons[i] == null) continue;
            
            int answerIndex = i; // capture untuk closure
            answerButtons[i].onClick.RemoveAllListeners();
            answerButtons[i].onClick.AddListener(() => OnAnswerSelected(answerIndex));
        }
    }

    /// <summary>
    /// Pilih soal secara random dari pool yang tersedia
    /// </summary>
    private QuizQuestion GetRandomQuestion()
    {
        if (questions == null || questions.Length == 0)
        {
            Debug.LogWarning("QuestionTrigger: Tidak ada soal yang tersedia!");
            return null;
        }

        // Jika semua soal sudah dipakai, reset pool
        if (availableQuestionIndices.Count == 0)
        {
            InitializeQuestionPool();
        }

        // Pilih random dari pool yang tersedia
        int randomPoolIndex = Random.Range(0, availableQuestionIndices.Count);
        int questionIndex = availableQuestionIndices[randomPoolIndex];
        
        // Hapus dari pool agar tidak muncul lagi
        availableQuestionIndices.RemoveAt(randomPoolIndex);

        return questions[questionIndex];
    }

    /// <summary>
    /// Tampilkan soal dan jawaban ke UI
    /// </summary>
    private void DisplayQuestion(QuizQuestion question)
    {
        if (question == null) return;

        // Set question text
        if (questionDisplayText != null)
        {
            questionDisplayText.text = question.questionText;
        }

        // Set answer button texts
        if (answerButtonTexts != null)
        {
            for (int i = 0; i < answerButtonTexts.Length && i < question.answerOptions.Length; i++)
            {
                if (answerButtonTexts[i] != null)
                {
                    // Format: "A. [jawaban]", "B. [jawaban]", dst.
                    char optionLetter = (char)('A' + i);
                    answerButtonTexts[i].text = $"{optionLetter}. {question.answerOptions[i]}";
                }
            }
        }
    }

    /// <summary>
    /// Dipanggil ketika player memilih jawaban
    /// </summary>
    private void OnAnswerSelected(int selectedIndex)
    {
        if (isAnswered || currentQuestion == null) return;

        if (selectedIndex == currentQuestion.correctAnswerIndex)
        {
            RightAnswer();
        }
        else
        {
            WrongAnswer();
        }
    }

    void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        pauseMenu.enabled = true;
        playerMovement.walkSpeed = originalWalkSpeed;
        playerMovement.runSpeed = originalRunSpeed;
        playerMovement.acceleration = originalAcceleration;
        playerMovement.jumpForce = originalJumpForce;
        playerMovement.sensitivityX = cameraSensitivity;
        playerMovement.sensitivityY = cameraSensitivity;
        crosshair.SetActive(true);
    }

    void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        pauseMenu.enabled = false;
        playerMovement.walkSpeed = 0f;
        playerMovement.runSpeed = 0f;
        playerMovement.acceleration = 0f;
        playerMovement.jumpForce = 0f;
        playerMovement.sensitivityX = 0f;
        playerMovement.sensitivityY = 0f;
        crosshair.SetActive(false);
    }

    public void OpenQuestion()
    {
        if (isAnswered) return;

        // Ambil soal random
        currentQuestion = GetRandomQuestion();
        if (currentQuestion == null) return;

        // Tampilkan soal ke UI
        DisplayQuestion(currentQuestion);

        // Buka panel
        questionPanel.SetActive(true);
        UnlockCursor();
        LeanTween.scale(questionPanel, Vector3.one, 0.5f).setEase(LeanTweenType.easeOutBack);
    }

    public void CloseQuestion()
    {
        LeanTween.scale(questionPanel, Vector3.zero, 0.5f).setEase(LeanTweenType.easeInBack).setOnComplete(() =>
        {
            questionPanel.SetActive(false);
            LockCursor();
        });
    }

    public void RightAnswer()
    {
        if (isAnswered) return;
        isAnswered = true;

        rightOrWrongTextPanel.SetActive(true);
        rightAnswerText.SetActive(true);

        onRightAnswer?.Invoke();

        LeanTween.scale(rightAnswerText, new Vector3(5.11f, 5.11f, 5.11f), 1f).setEase(LeanTweenType.easeOutSine).setOnComplete(() =>
        {
            CloseQuestion();
            rightOrWrongTextPanel.SetActive(false);
            rightAnswerText.SetActive(false);
            rightAnswerText.transform.localScale = Vector3.zero;
        });
    }

    public void WrongAnswer()
    {
        if (isAnswered) return;
        isAnswered = true;

        rightOrWrongTextPanel.SetActive(true);
        wrongAnswerText.SetActive(true);

        onWrongAnswer?.Invoke();

        LeanTween.scale(wrongAnswerText, new Vector3(5.11f, 5.11f, 5.11f), 1f).setEase(LeanTweenType.easeOutSine).setOnComplete(() =>
        {
            CloseQuestion();
            rightOrWrongTextPanel.SetActive(false);
            wrongAnswerText.SetActive(false);
            wrongAnswerText.transform.localScale = Vector3.zero;
        });
    }

    /// <summary>
    /// Reset quiz agar bisa dijawab ulang (opsional, untuk testing)
    /// </summary>
    public void ResetQuiz()
    {
        isAnswered = false;
        currentQuestion = null;
        InitializeQuestionPool();
    }

    /// <summary>
    /// Mendapatkan jumlah soal yang tersisa dalam pool
    /// </summary>
    public int GetRemainingQuestionsCount()
    {
        return availableQuestionIndices.Count;
    }
}
