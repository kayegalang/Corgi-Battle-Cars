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

        [Header("UI")]
        [SerializeField] private GameObject loadingScreen;

        [Header("Paw Progress")]
        [SerializeField] private Image pawImage;
        [SerializeField] private Sprite[] pawSprites; // 24 sprites

        private bool isLoading = false;
        private int lastIndex = -1;

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

            lastIndex = -1;
            UpdatePawProgress(0f);

            AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
            op.allowSceneActivation = false;

            float fakeProgress = 0f;

            // 🐾 FAKE LOADING (0 → 90)
            while (fakeProgress < 90f)
            {
                fakeProgress += Time.unscaledDeltaTime * 40f;

                float realProgress = Mathf.Clamp01(op.progress / 0.9f) * 90f;
                fakeProgress = Mathf.Min(fakeProgress, realProgress + 10f);

                UpdatePawProgress(fakeProgress / 100f);

                yield return null;
            }

            // Wait until Unity is ready
            while (op.progress < 0.9f)
                yield return null;

            // 🐾 FINAL FILL (90 → 100)
            while (fakeProgress < 100f)
            {
                fakeProgress += Time.unscaledDeltaTime * 100f;
                UpdatePawProgress(fakeProgress / 100f);
                yield return null;
            }

            yield return new WaitForSecondsRealtime(0.2f);

            // Activate scene
            op.allowSceneActivation = true;

            while (!op.isDone)
                yield return null;

            // Wait for GameplayManager
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

        // 🐾 Update paw sprite based on progress
        private void UpdatePawProgress(float progress01)
        {
            if (pawImage == null || pawSprites == null || pawSprites.Length == 0)
                return;

            int index = Mathf.Clamp(
                Mathf.FloorToInt(progress01 * pawSprites.Length),
                0,
                pawSprites.Length - 1
            );

            // Only update when stepping forward (prevents flicker)
            if (index != lastIndex)
            {
                pawImage.sprite = pawSprites[index];
                lastIndex = index;
            }
        }
    }
}