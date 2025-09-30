using UnityEngine;
using UnityEngine.SceneManagement;

namespace UI.Scripts
{
    public class PauseController : MonoBehaviour
    {
        public void OnResumeButtonClick()
        {
            GameplayManager.instance.UnpauseGame();
        }

        public void OnMainMenuButtonClick()
        {
            SceneManager.LoadScene("MainMenu");
        }

        public void OnQuitButtonClick()
        {
            #if UNITY_EDITOR 
                UnityEditor.EditorApplication.isPlaying = false;
            #else
                Application.Quit();
            #endif
        }
    
    }   
}
