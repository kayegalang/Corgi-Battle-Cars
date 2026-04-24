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
        
        private bool isLoading = false;

        public void LoadScene(string sceneName)
        {
            if (isLoading)
            {
                return;
            }
    
            isLoading = true;
            StartCoroutine(LoadSceneAsync(sceneName));
        }

        private IEnumerator LoadSceneAsync(string sceneName)
        {
            loadingScreen.SetActive(true);
            progressBar.value = 0f;
    
            AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
            op.allowSceneActivation = false;
    
            while (!op.isDone)
            {
                progressBar.value = Mathf.Clamp01(op.progress / 0.9f);
                yield return null;

                if (progressBar.value >= 0.9f)
                {
                    break;
                }
            }
    
            op.allowSceneActivation = true;
            yield return null;
            yield return new WaitForSeconds(0.5f);
    
            float timeout = 3f;
            float elapsed = 0f;
            while (!GameplayManager.instance.IsGameSetupComplete() && elapsed < timeout)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }
    
            loadingScreen.SetActive(false);
            yield return new WaitForSeconds(0.3f);
    
            GameplayManager.instance.StartMatchTimer();
    
            isLoading = false; 
        }
    }
}