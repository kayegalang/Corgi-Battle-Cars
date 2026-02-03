using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace _UI.Scripts
{
    public class PauseController : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject pauseScreen;
        [SerializeField] private GameObject gameplayPanel;
        
        [Header("Scene Names")]
        [SerializeField] private string mainMenuSceneName = "MainMenu";
        
        [Header("Events")]
        public UnityEvent onPaused;
        public UnityEvent onUnpaused;
        
        public bool IsPaused { get; private set; }

        public void PauseGame()
        {
            if (IsPaused)
            {
                return;
            }
            
            SetPauseState(true);
            onPaused?.Invoke();
        }
    
        public void UnpauseGame()
        {
            if (!IsPaused)
            {
                return;
            }
            
            SetPauseState(false);
            onUnpaused?.Invoke();
        }
        
        private void SetPauseState(bool paused)
        {
            IsPaused = paused;
            
            if (gameplayPanel != null)
            {
                gameplayPanel.SetActive(!paused);
            }
            
            if (pauseScreen != null)
            {
                pauseScreen.SetActive(paused);
            }
            
            Time.timeScale = paused ? 0 : 1;
            SetCursorState(paused);
        }
        
        private void SetCursorState(bool visible)
        {
            Cursor.visible = visible;
            Cursor.lockState = visible ? CursorLockMode.None : CursorLockMode.Locked;
            
            // Force the cursor state again next frame to ensure it sticks
            if (!visible)
            {
                StartCoroutine(ForceCursorHidden());
            }
        }
        
        private System.Collections.IEnumerator ForceCursorHidden()
        {
            yield return null; // Wait one frame
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
    
        public void OnMainMenuButtonClick()
        {
            Debug.Log($"[{nameof(PauseController)}] Returning to main menu");
            
            ResetGameState();
            CleanUpScene();
            
            SceneManager.LoadScene(mainMenuSceneName);
        }

        private void CleanUpScene()
        {
            GameObject playerInputManager = GameObject.Find("PlayerInputManager");
            
            if (playerInputManager != null)
            {
                Debug.Log($"[{nameof(PauseController)}] Destroying PlayerInputManager");
                Destroy(playerInputManager);
            }
        }

        private void ResetGameState()
        {
            Debug.Log($"[{nameof(PauseController)}] Resetting game state");
            
            Time.timeScale = 1;
            IsPaused = false;
            
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }

        public void OnQuitButtonClick()
        {
            #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
            #else
                Application.Quit();
            #endif
        }
        
        public bool GetIsPaused() => IsPaused;
    }
}