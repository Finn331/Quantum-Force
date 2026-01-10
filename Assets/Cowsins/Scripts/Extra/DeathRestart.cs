using UnityEngine;
using UnityEngine.SceneManagement;
namespace cowsins
{
    public class DeathRestart : MonoBehaviour
    {
        private void Update()
        {
            if (InputManager.reloading) 
            {
                 // Check if Checkpoint exists
                 if (SaveManager.instance != null && SaveManager.instance.GetSavedPosition().HasValue && !SaveManager.instance.resetOnStart)
                 {
                      string sceneToLoad = SaveManager.instance.GetSavedScene();
                      if(string.IsNullOrEmpty(sceneToLoad)) sceneToLoad = SceneManager.GetActiveScene().name;
                      SceneManager.LoadScene(sceneToLoad);
                 }
                 else
                 {
                      SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
                 }
            }
        }
    }
}