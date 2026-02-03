using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace _Gameplay.Scripts
{
    public class MatchTimerManager : MonoBehaviour
    {
        public static MatchTimerManager instance;
        
        [Header("Timer Settings")]
        [SerializeField] private int matchCountdownSeconds = 3;
        [SerializeField] private int gameDurationSeconds = 60;
        
        [Header("Timer Text")]
        [SerializeField] private string countdownGoText = "Go!";
        [SerializeField] private string gameTimerFormat = "Time: {0}";
        
        [Header("Events")]
        public UnityEvent onMatchStart;
        public UnityEvent onGameEnd;
        
        private TextMeshProUGUI matchTimerText;
        private TextMeshProUGUI gameTimerText;
        
        private const string MATCH_TIMER_OBJECT_NAME = "MatchTimerText";
        private const string GAME_TIMER_OBJECT_NAME = "GameTimerText";
        
        private void Awake()
        {
            InitializeSingleton();
        }
        
        private void InitializeSingleton()
        {
            if (instance == null)
            {
                instance = this;
            }
            else if (instance != this)
            {
                Destroy(this);
            }
        }
        
        public void StartMatchCountdown()
        {
            if (!FindTimerTextComponents())
            {
                return;
            }
            
            StartCoroutine(MatchCountdown());
        }
        
        private bool FindTimerTextComponents()
        {
            matchTimerText = GameObject.Find(MATCH_TIMER_OBJECT_NAME)?.GetComponent<TextMeshProUGUI>();
            
            if (matchTimerText == null)
            {
                Debug.LogError($"[{nameof(MatchTimerManager)}] {MATCH_TIMER_OBJECT_NAME} not found!");
                return false;
            }
            
            return true;
        }
        
        private IEnumerator MatchCountdown()
        {
            Time.timeScale = 0;
            HideCursor();
            
            int timeRemaining = matchCountdownSeconds;
            
            while (timeRemaining >= 0)
            {
                UpdateMatchTimerDisplay(timeRemaining);
                yield return new WaitForSecondsRealtime(1);
                timeRemaining--;
            }
            
            ClearMatchTimerDisplay();
            Time.timeScale = 1;
            
            OnMatchStart();
            StartGameDurationTimer();
        }
        
        private void HideCursor()
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
        
        private void UpdateMatchTimerDisplay(int timeRemaining)
        {
            if (timeRemaining == 0)
            {
                matchTimerText.text = countdownGoText;
            }
            else
            {
                matchTimerText.text = timeRemaining.ToString();
            }
        }
        
        private void ClearMatchTimerDisplay()
        {
            matchTimerText.text = "";
        }
        
        private void OnMatchStart()
        {
            Debug.Log($"[{nameof(MatchTimerManager)}] Match started - invoking onMatchStart event");
            onMatchStart?.Invoke();
        }
        
        private void StartGameDurationTimer()
        {
            if (!FindGameTimerText())
            {
                return;
            }
            
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
            int timeRemaining = duration;
            
            while (timeRemaining > 0)
            {
                UpdateGameTimerDisplay(timeRemaining);
                yield return new WaitForSeconds(1);
                timeRemaining--;
            }
            
            UpdateGameTimerDisplay(0);
            OnGameEnd();
        }
        
        private void UpdateGameTimerDisplay(int timeRemaining)
        {
            gameTimerText.text = string.Format(gameTimerFormat, timeRemaining);
        }
        
        private void OnGameEnd()
        {
            Debug.Log($"[{nameof(MatchTimerManager)}] Time's up - invoking onGameEnd event");
            onGameEnd?.Invoke();
        }
        
        public void StopAllTimers()
        {
            StopAllCoroutines();
        }
    }
}