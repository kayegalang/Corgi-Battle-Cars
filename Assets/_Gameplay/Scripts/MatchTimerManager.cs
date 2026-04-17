using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using _Audio.scripts;

namespace _Gameplay.Scripts
{
    public class MatchTimerManager : MonoBehaviour
    {
        public static MatchTimerManager instance;
        
        [Header("Timer Settings")]
        [SerializeField] private int matchCountdownSeconds = 3;
        [SerializeField] private int gameDurationSeconds   = 300;

        [Header("Music Timing")]
        [Tooltip("Seconds remaining when music breaks out of Main Loop toward Buildup")]
        [SerializeField] private int musicTransitionTime = 182;
        
        [Header("Timer Text")]
        [SerializeField] private string countdownGoText = "GO!";
        [SerializeField] private string gameTimerFormat = "Time: {0}";
        
        [Header("Events")]
        public UnityEvent onMatchStart;
        public UnityEvent onGameEnd;
        
        private TextMeshProUGUI gameTimerText;
        private bool            musicTransitionTriggered = false;
        
        private const string GAME_TIMER_OBJECT_NAME = "GameTimerText";
        
        // ═══════════════════════════════════════════════
        //  LIFECYCLE
        // ═══════════════════════════════════════════════

        private void Awake()
        {
            InitializeSingleton();
        }
        
        private void InitializeSingleton()
        {
            if (instance == null)
                instance = this;
            else if (instance != this)
            {
                Debug.LogWarning($"[MatchTimerManager] Duplicate found — destroying this one on {gameObject.name}!");
                Destroy(this);
            }
        }

        // ═══════════════════════════════════════════════
        //  COUNTDOWN
        // ═══════════════════════════════════════════════
        
        public void StartMatchCountdown()
        {
            StartCoroutine(MatchCountdown());
        }
        
        private IEnumerator MatchCountdown()
        {
            Time.timeScale = 0;
            HideCursor();

            for (int i = matchCountdownSeconds; i >= 1; i--)
            {
                if (CountdownUI.instance != null)
                    yield return StartCoroutine(CountdownUI.instance.ShowNumber(i));
                else
                    yield return new WaitForSecondsRealtime(1f);
            }

            if (CountdownUI.instance != null)
                yield return StartCoroutine(CountdownUI.instance.ShowGo(countdownGoText));
            else
                yield return new WaitForSecondsRealtime(0.5f);

            Time.timeScale = 1;
            OnMatchStart();
            StartGameDurationTimer();
        }
        
        private void HideCursor()
        {
            Cursor.visible   = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
        
        private void OnMatchStart()
        {
            onMatchStart?.Invoke();

            musicTransitionTriggered = false;
            MusicController.instance?.StartMusic();
            Debug.Log("[MatchTimerManager] Match started — music playing!");
        }

        // ═══════════════════════════════════════════════
        //  GAME TIMER
        // ═══════════════════════════════════════════════
        
        private void StartGameDurationTimer()
        {
            if (!FindGameTimerText()) return;
            StartCoroutine(GameDurationTimer(gameDurationSeconds));
        }
        
        private bool FindGameTimerText()
        {
            gameTimerText = GameObject.Find(GAME_TIMER_OBJECT_NAME)?.GetComponent<TextMeshProUGUI>();
            
            if (gameTimerText == null)
            {
                Debug.LogError($"[{nameof(MatchTimerManager)}] {GAME_TIMER_OBJECT_NAME} not found!");
                return false;
            }
            return true;
        }
        
        private IEnumerator GameDurationTimer(int duration)
        {
            if (GameTimerUI.instance != null)
            {
                GameTimerUI.instance.SetTotalDuration(duration);
                GameTimerUI.instance.StartTimer();
            }

            int timeRemaining = duration;

            while (timeRemaining > 0)
            {
                UpdateGameTimerDisplay(timeRemaining);

                if (GameTimerUI.instance != null)
                    GameTimerUI.instance.UpdateTimer(timeRemaining);

                // Trigger music transition at the right time
                if (timeRemaining == musicTransitionTime && !musicTransitionTriggered)
                {
                    musicTransitionTriggered = true;
                    Debug.Log($"[MatchTimerManager] Triggering music buildup at {timeRemaining}s remaining!");
                    MusicController.instance?.TriggerBuildup();
                }

                yield return new WaitForSeconds(1);
                timeRemaining--;
            }

            UpdateGameTimerDisplay(0);

            if (GameTimerUI.instance != null)
                GameTimerUI.instance.UpdateTimer(0);

            OnGameEnd();
        }

        private void UpdateGameTimerDisplay(int timeRemaining)
        {
            if (gameTimerText == null) return;

            int minutes = timeRemaining / 60;
            int seconds = timeRemaining % 60;
            gameTimerText.text = string.Format(gameTimerFormat,
                string.Format("{0}:{1:00}", minutes, seconds));
        }
        
        private void OnGameEnd()
        {
            MusicController.instance?.TriggerGameEnd();
            Debug.Log("[MatchTimerManager] Game ended!");
            onGameEnd?.Invoke();
        }
        
        public void StopAllTimers()
        {
            StopAllCoroutines();
        }
    }
}