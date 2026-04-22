using System.Collections;
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

        [Header("References")]
        [SerializeField] private PauseController pauseController;

        private EventInstance musicInstance;
        private bool          musicStarted;

        // ═══════════════════════════════════════════════
        //  LIFECYCLE
        // ═══════════════════════════════════════════════

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
            try
            {
                Debug.Log($"[MusicController] Using FMOD event path: {FMODEvents.instance.DogParkStart.Path}");
                musicInstance = RuntimeManager.CreateInstance(FMODEvents.instance.DogParkStart);
                musicInstance.setVolume(normalVolume);

                // Explicitly reset GameState to 0 on creation
                musicInstance.setParameterByName("GameState", 0f);
                Debug.Log("[MusicController] FMOD instance created, GameState = 0");
            }
            catch (EventNotFoundException e)
            {
                Debug.LogWarning($"[MusicController] FMOD event not found — banks not built yet. {e.Message}");
                return;
            }

            if (pauseController != null)
            {
                pauseController.onPaused.AddListener(HandlePaused);
                pauseController.onUnpaused.AddListener(HandleUnpaused);
            }
        }

        // ═══════════════════════════════════════════════
        //  PLAYBACK
        // ═══════════════════════════════════════════════

        public void StartMusic()
        {
            if (musicStarted) return;

            // Ensure GameState is 0 before starting
            musicInstance.setParameterByName("GameState", 0f);
            Debug.Log("[MusicController] StartMusic — GameState reset to 0");

            musicInstance.start();
            musicStarted = true;

            musicInstance.getPlaybackState(out PLAYBACK_STATE state);
            Debug.Log($"[MusicController] Music started! Playback state: {state}");
        }

        // ═══════════════════════════════════════════════
        //  MUSIC TRANSITIONS
        // ═══════════════════════════════════════════════

        /// <summary>
        /// Called at ~182 seconds remaining on the match timer.
        /// Sets GameState = 1 — FMOD stops looping the Main Loop
        /// and continues through Buildup → Solo → Outro → End Game Loop.
        /// </summary>
        public void TriggerBuildup()
        {
            if (!musicStarted) return;

            FMOD.RESULT result = musicInstance.setParameterByName("GameState", 1f);

            if (result == FMOD.RESULT.OK)
                Debug.Log("[MusicController] GameState = 1 — transitioning to Buildup!");
            else
                Debug.LogWarning($"[MusicController] Failed to set GameState: {result}");
        }

        /// <summary>
        /// Called when the match timer hits zero.
        /// Outro should already be playing by now.
        /// End Game Loop fires automatically after Outro.
        /// </summary>
        public void TriggerGameEnd()
        {
            if (!musicStarted) return;
            Debug.Log("[MusicController] Game ended — Outro should be playing, End Game Loop incoming!");
        }

        // ═══════════════════════════════════════════════
        //  PAUSE HANDLING
        //  Fully pauses/resumes the FMOD timeline so the
        //  music stays perfectly in sync with the game timer
        // ═══════════════════════════════════════════════

        private void HandlePaused()
        {
            musicInstance.setPaused(true);
            Debug.Log("[MusicController] Music paused.");
        }

        private void HandleUnpaused()
        {
            musicInstance.setPaused(false);
            Debug.Log("[MusicController] Music resumed.");
        }

        // ═══════════════════════════════════════════════
        //  CLEANUP
        // ═══════════════════════════════════════════════

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