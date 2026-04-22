using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using FMODUnity;
using _Audio.scripts;

namespace _Gameplay.Scripts
{
    /// <summary>
    /// Handles the visual animation of the match countdown (3, 2, 1, GO!).
    /// Called by MatchTimerManager. Add to a full-screen Canvas GameObject.
    /// </summary>
    public class CountdownUI : MonoBehaviour
    {
        public static CountdownUI instance;

        [Header("References")]
        [SerializeField] private TextMeshProUGUI countdownText;
        [SerializeField] private Image           screenFlash;

        [Header("Number Animation")]
        [Tooltip("Scale the text punches in from")]
        [SerializeField] private float punchStartScale  = 3f;
        [Tooltip("Slight overshoot scale before settling")]
        [SerializeField] private float overshootScale   = 0.85f;
        [Tooltip("How long each number is visible")]
        [SerializeField] private float holdDuration     = 0.4f;
        [Tooltip("How long the punch-in takes")]
        [SerializeField] private float punchInDuration  = 0.15f;
        [Tooltip("How long the settle takes after overshoot")]
        [SerializeField] private float settleDuration   = 0.1f;
        [Tooltip("How long the fade-out takes")]
        [SerializeField] private float fadeOutDuration  = 0.2f;

        [Header("GO! Animation")]
        [Tooltip("GO! flies upward by this amount")]
        [SerializeField] private float goFlyDistance    = 300f;
        [Tooltip("How long GO! takes to fly off screen")]
        [SerializeField] private float goFlyDuration    = 0.5f;

        [Header("Screen Flash")]
        [SerializeField] private Color  flashColor      = new Color(1f, 1f, 1f, 0.6f);
        [SerializeField] private float  flashDuration   = 0.15f;

        [Header("Colors")]
        [SerializeField] private Color color3 = new Color(0.9f, 0.2f, 0.2f); // red
        [SerializeField] private Color color2 = new Color(0.9f, 0.6f, 0.1f); // orange
        [SerializeField] private Color color1 = new Color(0.9f, 0.9f, 0.1f); // yellow
        [SerializeField] private Color colorGo = new Color(0.2f, 0.9f, 0.2f); // green
        
        [Header("FMOD Audio")]
        [SerializeField] private EventReference bark3sound;
        [SerializeField] private EventReference bark2sound;
        [SerializeField] private EventReference bark1sound;
        [SerializeField] private EventReference barkGosound;

        private Vector3 originalTextPosition;

        // ═══════════════════════════════════════════════
        //  LIFECYCLE
        // ═══════════════════════════════════════════════

        private void Awake()
        {
            if (instance == null) instance = this;
            else Destroy(gameObject);

            if (countdownText != null)
            {
                originalTextPosition      = countdownText.rectTransform.anchoredPosition;
                countdownText.gameObject.SetActive(false);
            }

            if (screenFlash != null)
            {
                screenFlash.color         = Color.clear;
                screenFlash.gameObject.SetActive(false);
            }
        }

        // ═══════════════════════════════════════════════
        //  PUBLIC API — called by MatchTimerManager
        // ═══════════════════════════════════════════════

        /// <summary>
        /// Call this for each countdown number (3, 2, 1).
        /// Returns a coroutine — yield it in MatchTimerManager to wait for it to finish.
        /// </summary>
        public IEnumerator ShowNumber(int number)
        {
            if (countdownText == null) yield break;
            
            switch (number)
            {
                case 3:
                    AudioManager.instance.PlayOneShot(FMODEvents.instance.bark3Sound, transform.position);
                    Debug.Log("bark 3");
                    break;
                case 2:
                    AudioManager.instance.PlayOneShot(FMODEvents.instance.bark2Sound, transform.position);
                    Debug.Log("bark 2");
                    break;
                case 1:
                    AudioManager.instance.PlayOneShot(FMODEvents.instance.bark1Sound, transform.position);
                    Debug.Log("bark 1");
                    break;
            }

            Color color = number == 3 ? color3 :
                          number == 2 ? color2 :
                                        color1;

            yield return StartCoroutine(PunchAnimate(number.ToString(), color, false));
        }

        /// <summary>
        /// Call this for GO!
        /// Returns a coroutine — yield it in MatchTimerManager to wait for it to finish.
        /// </summary>
        public IEnumerator ShowGo(string goText = "GO!")
        {
            if (countdownText == null) yield break;

            // Flash the screen
            StartCoroutine(FlashScreen());
            
            AudioManager.instance.PlayOneShot(FMODEvents.instance.barkGoSound, transform.position);

            yield return StartCoroutine(PunchAnimate(goText, colorGo, true));
        }

        // ═══════════════════════════════════════════════
        //  ANIMATION
        // ═══════════════════════════════════════════════

        private IEnumerator PunchAnimate(string text, Color color, bool flyOff)
        {
            // Setup
            countdownText.text  = text;
            countdownText.color = color;
            countdownText.rectTransform.anchoredPosition = originalTextPosition;
            countdownText.rectTransform.localScale       = Vector3.one * punchStartScale;
            countdownText.gameObject.SetActive(true);

            // ── Punch in ──
            float elapsed = 0f;
            while (elapsed < punchInDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / punchInDuration;
                float scale = Mathf.Lerp(punchStartScale, overshootScale, t);
                countdownText.rectTransform.localScale = Vector3.one * scale;
                yield return null;
            }

            // ── Settle to 1x ──
            elapsed = 0f;
            while (elapsed < settleDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / settleDuration;
                float scale = Mathf.Lerp(overshootScale, 1f, t);
                countdownText.rectTransform.localScale = Vector3.one * scale;
                yield return null;
            }

            countdownText.rectTransform.localScale = Vector3.one;

            // ── Hold ──
            yield return new WaitForSecondsRealtime(holdDuration);

            if (flyOff)
            {
                // ── GO! flies upward and fades out ──
                Vector2 startPos   = countdownText.rectTransform.anchoredPosition;
                Vector2 targetPos  = startPos + Vector2.up * goFlyDistance;
                Color   startColor = color;
                Color   endColor   = new Color(color.r, color.g, color.b, 0f);

                elapsed = 0f;
                while (elapsed < goFlyDuration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    float t = elapsed / goFlyDuration;
                    float easedT = 1f - Mathf.Pow(1f - t, 2f); // ease out

                    countdownText.rectTransform.anchoredPosition =
                        Vector2.Lerp(startPos, targetPos, easedT);
                    countdownText.color = Color.Lerp(startColor, endColor, easedT);

                    yield return null;
                }
            }
            else
            {
                // ── Regular number fades out ──
                Color   startColor = color;
                Color   endColor   = new Color(color.r, color.g, color.b, 0f);

                elapsed = 0f;
                while (elapsed < fadeOutDuration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    float t = elapsed / fadeOutDuration;
                    countdownText.color = Color.Lerp(startColor, endColor, t);
                    yield return null;
                }
            }

            countdownText.gameObject.SetActive(false);
            countdownText.rectTransform.anchoredPosition = originalTextPosition;
        }

        private IEnumerator FlashScreen()
        {
            if (screenFlash == null) yield break;

            screenFlash.gameObject.SetActive(true);
            screenFlash.color = flashColor;

            float elapsed = 0f;
            while (elapsed < flashDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / flashDuration;
                screenFlash.color = Color.Lerp(flashColor, Color.clear, t);
                yield return null;
            }

            screenFlash.color = Color.clear;
            screenFlash.gameObject.SetActive(false);
        }
    }
}
