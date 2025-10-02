using UnityEngine;

namespace UI.Scripts
{
    using UnityEngine;
    using UnityEngine.SceneManagement;
    using UnityEngine.UI;
    using System.Collections;

    public class LoadingManager : MonoBehaviour
    {
        public static LoadingManager instance;
        
        [SerializeField] private GameObject loadingScreen;
        [SerializeField] private Slider progressBar; 

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
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
                float progress = Mathf.Clamp01(op.progress / 0.9f);
                if (progressBar != null)
                    progressBar.value = progress;
                
                if (op.progress >= 0.9f)
                {

                    op.allowSceneActivation = true;
 
                }

                yield return null;
            }

            loadingScreen.SetActive(false);
        }
    }

}

