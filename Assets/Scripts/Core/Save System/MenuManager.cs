using UnityEngine;

public class MenuManager : MonoBehaviour
{
    [SerializeField] AudioClip clickSFX;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void BackToMainMenu()
    {
        // Load the main menu scene
        UnityEngine.SceneManagement.SceneManager.LoadScene("SampleScene");
    }
}
