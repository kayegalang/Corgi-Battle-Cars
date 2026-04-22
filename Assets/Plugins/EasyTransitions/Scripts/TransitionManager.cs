using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EasyTransition
{
    public static class TransitionManager
    {
        private const string PREFAB_PATH = "EasyTransitions/TransitionTemplate";
        
        private static TransitionPlayer _player;
        public static TransitionSettings DefaultTransitionSettings => _player.DefaultTransitionSettings;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            GameObject prefab = Resources.Load<GameObject>(PREFAB_PATH);
            if (prefab == null)
            {
                Debug.LogError($"[TransitionManager] Could not load transition prefab at Resources/{PREFAB_PATH}");
                return;
            }

            GameObject instance = UnityEngine.Object.Instantiate(prefab);
            instance.name = "TransitionTemplate";
            _player = instance.GetComponent<TransitionPlayer>();
            UnityEngine.Object.DontDestroyOnLoad(instance);
        }

        /// <summary>
        /// A full screen transition to a black screen.
        /// </summary>
        /// <param name="settings"></param>
        /// <param name="completion"></param>
        public static void PlayTransitionIn(TransitionSettings settings = null, Action completion = null)
        {
            if (settings == null) settings = DefaultTransitionSettings;
            
            _player.PlayIn(settings, completion);
        }

        /// <summary>
        /// A full screen transition from a black screen.
        /// </summary>
        /// <param name="settings"></param>
        /// <param name="completion"></param>
        public static void PlayTransitionOut(TransitionSettings settings = null, Action completion = null)
        {
            if (settings == null) settings = DefaultTransitionSettings;
            
            _player.PlayOut(settings, completion);
        }
        
        /// <summary>
        /// Transition to black screen -> Load scene async -> transition from black screen.
        /// </summary>
        /// <param name="sceneIndex"></param>
        /// <param name="settings"></param>
        /// <param name="completion"></param>
        public static void LoadSceneAsyncWithTransition(string sceneName, TransitionSettings settings = null, Action onSceneLoaded = null)
        {
            if (settings == null) settings = DefaultTransitionSettings;

            PlayTransitionIn(settings, () =>
            {
                LoadSceneAsync(sceneName, onSceneLoaded);
            });
        }

        public static void LoadSceneAsync(string sceneName, Action onSceneLoaded = null)
        {
            AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
            if (op == null)
            {
                throw new Exception($"[TransitionManager] LoadSceneAsync(\"{sceneName}\") failed! Check scene name and ensure it is in build settings.");
            }

            op.allowSceneActivation = true;
            op.completed += _ =>
            {
                onSceneLoaded?.Invoke();
            };
        }
    }
}
