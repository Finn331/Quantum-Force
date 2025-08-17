using cowsins;
using TMPro;
using UnityEngine;

public class QuestionTrigger : MonoBehaviour
{
    [Header("Question Settings")]
    [SerializeField] GameObject questionPanel;
    [SerializeField] AudioClip panelOpenSFX;
    [SerializeField] GameObject rightOrWrongTextPanel;
    [SerializeField] GameObject rightAnswerText;
    [SerializeField] GameObject wrongAnswerText;

    [Header("Player Reference")]
    [SerializeField] PlayerMovement playerMovement;
    [SerializeField] PauseMenu pauseMenu;
    [SerializeField] GameObject crosshair;

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
        LeanTween.scale(wrongAnswerText, new Vector3(5.1102f, 5.1102f, 5.1102f), 1f).setEase(LeanTweenType.easeOutSine).setOnComplete(() =>
        {
            CloseQuestion();
            rightOrWrongTextPanel.SetActive(false);
            wrongAnswerText.SetActive(false);
            wrongAnswerText.transform.localScale = Vector3.zero;
        });
    }
}