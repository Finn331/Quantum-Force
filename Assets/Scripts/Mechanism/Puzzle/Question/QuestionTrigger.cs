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
    private float originalWalkSpeed = 5f; // Default walk speed
    private float originalRunSpeed = 10f; // Default run speed
    private float originalAcceleration = 4500f; // Default acceleration
    private float originalJumpForce = 10f; // Default jump force
    private float cameraSensitivity = 4f; // Default camera sensitivity

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        pauseMenu.enabled = true; // Enable pause menu to allow interaction
        playerMovement.walkSpeed = originalWalkSpeed; // Restore player speed
        playerMovement.runSpeed = originalRunSpeed; // Restore run speed
        playerMovement.acceleration = originalAcceleration; // Restore acceleration
        playerMovement.jumpForce = originalJumpForce; // Restore jump force
        playerMovement.sensitivityX = cameraSensitivity; // Restore camera sensitivity
        playerMovement.sensitivityY = cameraSensitivity; // Restore camera sensitivity
        crosshair.SetActive(true); // Show the crosshair when the question is open
    }

    void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;        
        pauseMenu.enabled = false; // Disable pause menu to prevent interaction while question is open
        playerMovement.walkSpeed = 0f; // Set player speed to 0 to prevent movement while the question is open
        playerMovement.runSpeed = 0f; // Set run speed to 0 as well
        playerMovement.acceleration = 0f; // Set acceleration to 0 to prevent any movement input
        playerMovement.jumpForce = 0f;
        playerMovement.sensitivityX = 0f; // Set camera sensitivity to 0 to prevent camera movement
        playerMovement.sensitivityY = 0f; // Set camera sensitivity to 0 to prevent camera movement
        crosshair.SetActive(false); // Hide the crosshair when the question is open
    }

    public void OpenQuestion()
    {

        questionPanel.SetActive(true); // Show the question panel
        UnlockCursor(); // Unlock the cursor for interaction
        LeanTween.scale(questionPanel, Vector3.one, 0.5f).setEase(LeanTweenType.easeOutBack); // Animate the panel opening

    }

    public void CloseQuestion()
    {
        LeanTween.scale(questionPanel, Vector3.zero, 0.5f).setEase(LeanTweenType.easeInBack).setOnComplete(() =>
        {

            questionPanel.SetActive(false); // Hide the question panel
            LockCursor(); // Lock the cursor again
        });
    }

    public void RightAnswer()
    {        
        rightOrWrongTextPanel.SetActive(true); // Show the right or wrong text panel
        rightAnswerText.SetActive(true); // Show the right answer text
        LeanTween.scale(rightAnswerText, new Vector3(5.1102f, 5.1102f, 5.1102f), 1).setEase(LeanTweenType.easeOutSine).setOnComplete(() =>
        {
            CloseQuestion();
            rightOrWrongTextPanel.SetActive(false); // Hide the right or wrong text panel
            rightAnswerText.SetActive(false); // Hide the right answer text
            rightAnswerText.transform.localScale = Vector3.zero; // Reset scale to zero for next time
        });
    }

    public void WrongAnswer()
    {
        rightOrWrongTextPanel.SetActive(true); // Show the right or wrong text panel
        wrongAnswerText.SetActive(true); // Show the wrong answer text
        LeanTween.scale(wrongAnswerText, new Vector3(5.1102f, 5.1102f, 5.1102f), 1f).setEase(LeanTweenType.easeOutSine).setOnComplete(() =>
        {
            CloseQuestion();
            rightOrWrongTextPanel.SetActive(false); // Hide the right or wrong text panel
            wrongAnswerText.SetActive(false); // Hide the wrong answer text
            wrongAnswerText.transform.localScale = Vector3.zero; // Reset scale to zero for next time
        });
    }
}