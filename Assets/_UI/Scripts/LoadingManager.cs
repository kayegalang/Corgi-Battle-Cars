using _Gameplay.Scripts;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

namespace _UI.Scripts
{
    public class LoadingManager : MonoBehaviour
    {
        public static LoadingManager instance;
        
        [SerializeField] private GameObject loadingScreen;
        [SerializeField] private Slider progressBar; 

        void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
                Destroy(gameObject);
        }
        public void LoadScene(string sceneName)
        {
            StartCoroutine(LoadSceneAsync(sceneName));
        }

        private IEnumerator LoadSceneAsync(string sceneName)
        {
            loadingScreen.SetActive(true);
            
            AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
            op.allowSceneActivation = false;
            
            while (!op.isDone)
            {
                float targetProgress = Mathf.Clamp01(op.progress / 0.9f);
                progressBar.value = Mathf.MoveTowards(progressBar.value, targetProgress, Time.deltaTime * 0.25f);
                yield return null;

                if (progressBar.value >= 1f)
                {
                    break;
                }
            }
            
            op.allowSceneActivation = true;
            
            // Wait for scene to actually be active
            yield return null;
            
            // Give the scene time to initialize
            yield return new WaitForSeconds(0.5f);
            
            // Wait for game setup with a timeout safety
            float timeout = 3f;
            float elapsed = 0f;
            while (!GameplayManager.instance.IsGameSetupComplete() && elapsed < timeout)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            if (elapsed >= timeout)
            {
                Debug.LogWarning("Game setup timed out!");
            }
            
            loadingScreen.SetActive(false);
            
            yield return new WaitForSeconds(0.3f);
            
            GameplayManager.instance.StartMatchTimer();
        }
    }
}