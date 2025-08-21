using cowsins;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

public class QuestionTrigger : MonoBehaviour
{
    [Header("Question Settings")]
    [SerializeField] private GameObject questionPanel;
    [SerializeField] private AudioClip panelOpenSFX;
    [SerializeField] private GameObject rightOrWrongTextPanel;
    [SerializeField] private GameObject rightAnswerText;
    [SerializeField] private GameObject wrongAnswerText;

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

    void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        pauseMenu.enabled = true; // Enable pause menu to allow interaction
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
        pauseMenu.enabled = false; // Disable pause menu to prevent interaction while question is open
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
        rightOrWrongTextPanel.SetActive(true);
        rightAnswerText.SetActive(true);

        // panggil event untuk jawaban benar
        onRightAnswer?.Invoke();

        LeanTween.scale(rightAnswerText, new Vector3(5.1102f, 5.1102f, 5.1102f), 1).setEase(LeanTweenType.easeOutSine).setOnComplete(() =>
        {
            CloseQuestion();
            rightOrWrongTextPanel.SetActive(false);
            rightAnswerText.SetActive(false);
            rightAnswerText.transform.localScale = Vector3.zero;
        });
    }

    public void WrongAnswer()
    {
        rightOrWrongTextPanel.SetActive(true);
        wrongAnswerText.SetActive(true);

        // panggil event untuk jawaban salah
        onWrongAnswer?.Invoke();

        LeanTween.scale(wrongAnswerText, new Vector3(5.1102f, 5.1102f, 5.1102f), 1f).setEase(LeanTweenType.easeOutSine).setOnComplete(() =>
        {
            CloseQuestion();
            rightOrWrongTextPanel.SetActive(false);
            wrongAnswerText.SetActive(false);
            wrongAnswerText.transform.localScale = Vector3.zero;
        });
    }
}
