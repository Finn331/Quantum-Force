using UnityEngine;

public class TutorialTrigger : MonoBehaviour
{
    [Header("Trigger Properties")]
    [Tooltip("UI Panel that you want to trigger.")]
    [SerializeField] GameObject tutorialPanel;
    [Tooltip("Text that you want to animate.")]
    [SerializeField] GameObject tutorialText;

    private Collider tutorialTrigger;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        tutorialTrigger = GetComponent<Collider>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            TriggerTutorial();
        }
    }

    public void TriggerTutorial()
    {
        if (tutorialPanel != null)
        {
            tutorialPanel.SetActive(true);
            tutorialTrigger.enabled = false; // Disable the trigger to prevent multiple activations
        }
        if (tutorialText != null)
        {
            LeanTween.scale(tutorialText, Vector3.one, 2f).setEase(LeanTweenType.easeOutBounce).setOnComplete(() =>
            {
                // Optionally, you can add more actions after the animation completes
                Debug.Log("Tutorial text animation completed.");
                LeanTween.scale(tutorialText, Vector3.zero, 2f).setEase(LeanTweenType.easeInBounce).setOnComplete(() =>
                {
                    // Optionally, you can deactivate the tutorial text after the animation
                    tutorialPanel.SetActive(false);
                    Destroy(this.gameObject); // Destroy the trigger object after the tutorial is shown
                });
            });
        }
    }
}
