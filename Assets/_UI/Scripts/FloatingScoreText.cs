using System.Collections;
using TMPro;
using UnityEngine;

namespace _UI.Scripts
{
    /// <summary>
    /// Spawned by PlayerUIManager when a kill is scored.
    /// Floats upward and fades out, then destroys itself.
    /// </summary>
    public class FloatingScoreText : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI floatingText;

        [Header("Animation")]
        [SerializeField] private float floatDistance = 80f;
        [SerializeField] private float duration      = 1.2f;
        [SerializeField] private float holdDuration  = 0.3f;
        [SerializeField] private Color textColor     = new Color(1f, 0.9f, 0.2f); // bright yellow

        // ═══════════════════════════════════════════════
        //  PUBLIC API
        // ═══════════════════════════════════════════════

        public void Play(string text)
        {
            if (floatingText != null)
                floatingText.text = text;

            StartCoroutine(Animate());
        }

        // ═══════════════════════════════════════════════
        //  ANIMATION
        // ═══════════════════════════════════════════════

        private IEnumerator Animate()
        {
            if (floatingText == null)
            {
                Destroy(gameObject);
                yield break;
            }

            RectTransform rt    = floatingText.rectTransform;
            Vector2       start = rt.anchoredPosition;
            Vector2       end   = start + Vector2.up * floatDistance;

            // Start fully visible
            floatingText.color = textColor;

            // Float up
            float elapsed = 0f;
            float moveDuration = duration - holdDuration;

            while (elapsed < moveDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / moveDuration;

                // Ease out — fast at start, slow at end
                float easedT = 1f - Mathf.Pow(1f - t, 2f);

                rt.anchoredPosition = Vector2.Lerp(start, end, easedT);

                // Fade out in the second half
                float fadeT = Mathf.InverseLerp(moveDuration * 0.4f, moveDuration, elapsed);
                Color c     = textColor;
                c.a         = Mathf.Lerp(1f, 0f, fadeT);
                floatingText.color = c;

                yield return null;
            }

            Destroy(gameObject);
        }
    }
}
