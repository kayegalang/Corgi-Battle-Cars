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

        private bool isLoading = false;

        void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);

                if (loadingScreen != null)
                    DontDestroyOnLoad(loadingScreen);
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            if (loadingScreen != null)
                loadingScreen.SetActive(false);
        }

        public void LoadScene(string sceneName)
        {
            if (isLoading) return;

            isLoading = true;
            StartCoroutine(LoadSceneAsync(sceneName));
        }

        private IEnumerator LoadSceneAsync(string sceneName)
        {
            if (loadingScreen != null)
                loadingScreen.SetActive(true);

            if (progressBar != null)
                progressBar.value = 0f;

            AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
            op.allowSceneActivation = false;

            float fakeProgress = 0f;

            while (fakeProgress < 90f)
            {
                // Move smoothly toward 90%
                fakeProgress += Time.unscaledDeltaTime * 80f; // speed control

                // Don't go past real progress
                float realProgress = Mathf.Clamp01(op.progress / 0.9f) * 90f;
                fakeProgress = Mathf.Min(fakeProgress, realProgress + 10f);

                if (progressBar != null)
                    progressBar.value = fakeProgress;

                yield return null;
            }

            // Wait until Unity is actually ready
            while (op.progress < 0.9f)
                yield return null;

            // Smooth fill from 90 → 100
            while (fakeProgress < 100f)
            {
                fakeProgress += Time.unscaledDeltaTime * 100f;

                if (progressBar != null)
                    progressBar.value = fakeProgress;

                yield return null;
            }

            yield return new WaitForSecondsRealtime(0.2f);

            // Activate scene
            op.allowSceneActivation = true;

            while (!op.isDone)
                yield return null;

            yield return new WaitUntil(() => GameplayManager.instance != null);

            float timeout = 3f;
            float elapsed = 0f;

            while (!GameplayManager.instance.IsGameSetupComplete() && elapsed < timeout)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            yield return new WaitForSecondsRealtime(0.2f);

            GameplayManager.instance.StartMatchTimer();

            if (loadingScreen != null)
                loadingScreen.SetActive(false);

            isLoading = false;
        }
    }
}