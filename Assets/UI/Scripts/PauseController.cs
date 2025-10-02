using UnityEngine;
using UnityEngine.SceneManagement;

namespace UI.Scripts
{
    public class PauseController : MonoBehaviour
    {
        public GameObject PauseScreen;
        public GameObject GameplayPanel;
    
        private bool isPaused = false;

        public void PauseGame()
        {
            GameplayPanel.SetActive(false);
            PauseScreen.SetActive(true);
            Time.timeScale = 0;
            SetIsPaused(true);
        }
    
        public void UnpauseGame()
        {
            GameplayPanel.SetActive(true);
            PauseScreen.SetActive(false);
            Time.timeScale = 1;
            SetIsPaused(false);
        }

        public bool GetIsPaused()
        {
            return isPaused;
        }

        public void SetIsPaused(bool value)
        {
            isPaused = value;
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