using UnityEngine;
using FMOD.Studio;
using FMODUnity;
using _UI.Scripts;

namespace _Audio.scripts
{
   public class MusicController : MonoBehaviour
{
    public static MusicController instance { get; private set; }

    [Header("Music Settings")]
    [SerializeField] private float normalVolume = 1f;
    [SerializeField] private float pausedVolume = 0.25f;

    [Header("Fade Settings")]
    [SerializeField] private float fadeDuration = 0f;
    
    [Header("References")]
    [SerializeField] private PauseController pauseController;

    private EventInstance musicInstance;
    private bool musicStarted;

    private Coroutine fadeCoroutine;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Debug.LogError("More than one MusicController found in the scene.");
            Destroy(gameObject);
            return;
        }

        instance = this;
    }

    private void Start()
    {
        musicInstance = RuntimeManager.CreateInstance(FMODEvents.instance.DogParkStart);
        musicInstance.setVolume(normalVolume);

        if (pauseController != null)
        {
            pauseController.onPaused.AddListener(HandlePaused);
            pauseController.onUnpaused.AddListener(HandleUnpaused);
        }
    }

    public void StartMusic()
    {
        if (musicStarted)
            return;

        musicInstance.start();
        musicStarted = true;
    }

    // ─────────────────────────────
    // FADE LOGIC
    // ─────────────────────────────

    private void HandlePaused()
    {
        StartFade(pausedVolume);
    }

    private void HandleUnpaused()
    {
        StartFade(normalVolume);
    }

    private void StartFade(float targetVolume)
    {
        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);

        fadeCoroutine = StartCoroutine(FadeVolume(targetVolume));
    }

    private System.Collections.IEnumerator FadeVolume(float targetVolume)
    {
        musicInstance.getVolume(out float startVolume);

        float elapsed = 0f;
        
        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            float t = elapsed / fadeDuration;
            float newVolume = Mathf.Lerp(startVolume, targetVolume, t);

            musicInstance.setVolume(newVolume);

            yield return null;
        }

        musicInstance.setVolume(targetVolume);
    }

    private void OnDestroy()
    {
        if (pauseController != null)
        {
            pauseController.onPaused.RemoveListener(HandlePaused);
            pauseController.onUnpaused.RemoveListener(HandleUnpaused);
        }

        musicInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
        musicInstance.release();
    }
}
}
