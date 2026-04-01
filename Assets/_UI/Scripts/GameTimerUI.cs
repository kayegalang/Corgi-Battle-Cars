using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _Gameplay.Scripts
{
    /// <summary>
    /// Handles all visual effects for the game timer.
    /// Called by MatchTimerManager each second.
    /// Add to a GameObject in each gameplay scene alongside CountdownUI.
    /// </summary>
    public class GameTimerUI : MonoBehaviour
    {
        public static GameTimerUI instance;

        [Header("References")]
        [SerializeField] private TextMeshProUGUI timerText;
        [SerializeField] private Image           vignetteImage;

        [Header("Timer Format")]
        [SerializeField] private string timerFormat = "{0}:{1:00}";

        [Header("Color Settings")]
        [SerializeField] private Color colorNormal  = Color.white;
        [SerializeField] private Color colorWarning = new Color(1f, 0.6f, 0.1f);  // orange
        [SerializeField] private Color colorDanger  = new Color(1f, 0.15f, 0.15f); // red
        [SerializeField] private int   warningThreshold = 90;  // seconds — go orange
        [SerializeField] private int   dangerThreshold  = 30;  // seconds — go red

        [Header("Pulse Settings")]
        [SerializeField] private int   pulseThreshold   = 30;  // seconds — start pulsing
        [SerializeField] private float pulseScale       = 1.15f;
        [SerializeField] private float pulseDuration    = 0.15f;

        [Header("Vignette Settings")]
        [SerializeField] private int   vignetteThreshold = 30; // seconds — vignette appears
        [SerializeField] private Color vignetteColor      = new Color(0.5f, 0f, 0f, 0f);
        [SerializeField] private float vignetteMaxAlpha   = 0.35f;

        [Header("Last 10 Seconds")]
        [SerializeField] private int   lastSecondsThreshold = 10;
        [SerializeField] private float tickPunchScale        = 1.3f;
        [SerializeField] private float tickPunchDuration     = 0.12f;
        [SerializeField] private AudioClip tickSound;
        [SerializeField] private AudioClip finalTickSound;
        private AudioSource audioSource;

        private int      totalDuration   = 300;
        private bool     isRunning       = false;
        private Coroutine pulseCoroutine  = null;

        // ═══════════════════════════════════════════════
        //  LIFECYCLE
        // ═══════════════════════════════════════════════

        private void Awake()
        {
            if (instance == null) instance = this;
            else Destroy(gameObject);

            audioSource = GetComponent<AudioSource>();
            if (audioSource == null && (tickSound != null || finalTickSound != null))
                audioSource = gameObject.AddComponent<AudioSource>();

            if (vignetteImage != null)
                vignetteImage.color = Color.clear;
        }

        // ═══════════════════════════════════════════════
        //  PUBLIC API — called by MatchTimerManager
        // ═══════════════════════════════════════════════

        public void SetTotalDuration(int seconds)
        {
            totalDuration = seconds;
        }

        public void StartTimer()
        {
            isRunning = true;
        }

        public void StopTimer()
        {
            isRunning = false;
        }

        /// <summary>
        /// Call this every second from MatchTimerManager with the remaining time.
        /// </summary>
        public void UpdateTimer(int secondsRemaining)
        {
            if (!isRunning) return;

            UpdateTimerText(secondsRemaining);
            UpdateColor(secondsRemaining);
            UpdateVignette(secondsRemaining);

            if (secondsRemaining <= pulseThreshold && secondsRemaining > lastSecondsThreshold)
                TriggerPulse();

            if (secondsRemaining <= lastSecondsThreshold && secondsRemaining > 0)
                TriggerLastSecondTick(secondsRemaining);

            if (secondsRemaining == 0)
                TriggerFinalTick();
        }

        // ═══════════════════════════════════════════════
        //  TIMER TEXT
        // ═══════════════════════════════════════════════

        private void UpdateTimerText(int secondsRemaining)
        {
            if (timerText == null) return;

            int minutes = secondsRemaining / 60;
            int seconds = secondsRemaining % 60;
            timerText.text = string.Format(timerFormat, minutes, seconds);
        }

        // ═══════════════════════════════════════════════
        //  COLOR
        // ═══════════════════════════════════════════════

        private void UpdateColor(int secondsRemaining)
        {
            if (timerText == null) return;

            Color target = secondsRemaining <= dangerThreshold  ? colorDanger  :
                           secondsRemaining <= warningThreshold ? colorWarning :
                                                                  colorNormal;

            timerText.color = target;
        }

        // ═══════════════════════════════════════════════
        //  VIGNETTE
        // ═══════════════════════════════════════════════

        private void UpdateVignette(int secondsRemaining)
        {
            if (vignetteImage == null) return;

            if (secondsRemaining > vignetteThreshold)
            {
                vignetteImage.color = Color.clear;
                return;
            }

            // Fade in as time runs out — max alpha at 0 seconds
            float t     = 1f - (float)secondsRemaining / vignetteThreshold;
            float alpha = Mathf.Lerp(0f, vignetteMaxAlpha, t);
            vignetteImage.color = new Color(vignetteColor.r, vignetteColor.g, vignetteColor.b, alpha);
        }

        // ═══════════════════════════════════════════════
        //  PULSE
        // ═══════════════════════════════════════════════

        private void TriggerPulse()
        {
            if (pulseCoroutine != null)
                StopCoroutine(pulseCoroutine);

            pulseCoroutine = StartCoroutine(PulseCoroutine(pulseScale));
        }

        private IEnumerator PulseCoroutine(float targetScale)
        {
            if (timerText == null) yield break;

            // Scale up
            float elapsed = 0f;
            while (elapsed < pulseDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / pulseDuration;
                timerText.rectTransform.localScale = Vector3.Lerp(Vector3.one, Vector3.one * targetScale, t);
                yield return null;
            }

            // Scale back down
            elapsed = 0f;
            while (elapsed < pulseDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / pulseDuration;
                timerText.rectTransform.localScale = Vector3.Lerp(Vector3.one * targetScale, Vector3.one, t);
                yield return null;
            }

            timerText.rectTransform.localScale = Vector3.one;
            pulseCoroutine = null;
        }

        // ═══════════════════════════════════════════════
        //  LAST 10 SECONDS
        // ═══════════════════════════════════════════════

        private void TriggerLastSecondTick(int secondsRemaining)
        {
            // Bigger punch for last 10 seconds
            if (pulseCoroutine != null)
                StopCoroutine(pulseCoroutine);

            pulseCoroutine = StartCoroutine(PulseCoroutine(tickPunchScale));

            // Play tick sound
            if (audioSource != null && tickSound != null)
                audioSource.PlayOneShot(tickSound);
        }

        private void TriggerFinalTick()
        {
            if (audioSource != null && finalTickSound != null)
                audioSource.PlayOneShot(finalTickSound);

            // Reset vignette and color
            if (vignetteImage != null)
                StartCoroutine(FadeOutVignette());
        }

        private IEnumerator FadeOutVignette()
        {
            float elapsed = 0f;
            float duration = 1f;
            Color startColor = vignetteImage.color;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                vignetteImage.color = Color.Lerp(startColor, Color.clear, t);
                yield return null;
            }

            vignetteImage.color = Color.clear;
        }
    }
}
