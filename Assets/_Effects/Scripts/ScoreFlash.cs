using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace _UI.Scripts
{
    /// <summary>
    /// Displays a colored edge flash around the player's viewport when they score.
    /// Uses playerCamera.rect to constrain edges to the correct split-screen area
    /// — same approach as PlayerUIManager.SetAnchors() for score text positioning.
    ///
    /// Add to each player's PlayerUICanvas.
    /// Call Flash() or FlashCrash() from PlayerUIManager when points are awarded.
    /// </summary>
    public class ScoreFlash : MonoBehaviour
    {
        [Header("Flash Settings")]
        [Tooltip("Color of the edge flash — set to the player's collar color")]
        [SerializeField] private Color flashColor      = Color.green;

        [Tooltip("Special crash kill color — orange/gold")]
        [SerializeField] private Color crashFlashColor = new Color(1f, 0.6f, 0f, 1f);

        [Tooltip("How long the flash lasts in seconds")]
        [SerializeField] private float flashDuration   = 0.5f;

        [Tooltip("Border thickness as a fraction of viewport height")]
        [SerializeField] [Range(0.01f, 0.15f)] private float borderThickness = 0.04f;

        [Tooltip("Peak alpha at the start of the flash")]
        [SerializeField] [Range(0f, 1f)] private float peakAlpha = 0.8f;

        // ─── refs ────────────────────────────────────────
        private Camera   playerCamera;
        private Image[]  edges = new Image[4]; // top, bottom, left, right
        private Coroutine flashCoroutine;

        // ═══════════════════════════════════════════════
        //  LIFECYCLE
        // ═══════════════════════════════════════════════

        private void Start()
        {
            // Find camera from root — same as CarShooter pattern
            playerCamera = transform.root.GetComponentInChildren<Camera>();

            if (playerCamera == null)
                Debug.LogWarning("[ScoreFlash] No camera found on player root!");

            BuildEdges();
            UpdateEdgeAnchors();
        }

        private void LateUpdate()
        {
            // Keep anchors in sync in case viewport changes (e.g. PresentationLayout toggle)
            UpdateEdgeAnchors();
        }

        // ═══════════════════════════════════════════════
        //  BUILD EDGES
        // ═══════════════════════════════════════════════

        private void BuildEdges()
        {
            string[] names = { "FlashTop", "FlashBottom", "FlashLeft", "FlashRight" };

            for (int i = 0; i < 4; i++)
            {
                GameObject go  = new GameObject(names[i]);
                go.transform.SetParent(transform, false);
                go.transform.SetAsLastSibling();

                go.AddComponent<RectTransform>();
                Image img         = go.AddComponent<Image>();
                img.raycastTarget = false;
                img.color         = Color.clear;
                edges[i]          = img;
            }
        }

        // ═══════════════════════════════════════════════
        //  ANCHOR EDGES TO VIEWPORT RECT
        //  Uses playerCamera.rect exactly like PlayerUIManager.SetAnchors()
        //  uses it for score text — constrains UI to the correct split-screen area
        // ═══════════════════════════════════════════════

        private void UpdateEdgeAnchors()
        {
            if (playerCamera == null || edges[0] == null) return;

            Rect vp = playerCamera.rect; // normalised 0-1 viewport rect

            // How thick each border strip is as a fraction of the viewport
            float thickX = vp.width  * borderThickness;
            float thickY = vp.height * borderThickness;

            // Top edge
            SetEdgeAnchors(edges[0],
                new Vector2(vp.xMin,         vp.yMax - thickY),
                new Vector2(vp.xMax,         vp.yMax));

            // Bottom edge
            SetEdgeAnchors(edges[1],
                new Vector2(vp.xMin,         vp.yMin),
                new Vector2(vp.xMax,         vp.yMin + thickY));

            // Left edge
            SetEdgeAnchors(edges[2],
                new Vector2(vp.xMin,         vp.yMin),
                new Vector2(vp.xMin + thickX, vp.yMax));

            // Right edge
            SetEdgeAnchors(edges[3],
                new Vector2(vp.xMax - thickX, vp.yMin),
                new Vector2(vp.xMax,          vp.yMax));
        }

        private void SetEdgeAnchors(Image edge, Vector2 anchorMin, Vector2 anchorMax)
        {
            RectTransform rt  = edge.transform as RectTransform;
            rt.anchorMin      = anchorMin;
            rt.anchorMax      = anchorMax;
            rt.offsetMin      = Vector2.zero;
            rt.offsetMax      = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;
        }

        // ═══════════════════════════════════════════════
        //  PUBLIC API
        // ═══════════════════════════════════════════════

        /// <summary>Normal kill flash — uses flashColor.</summary>
        public void Flash()
        {
            TriggerFlash(flashColor);
        }

        /// <summary>Crash kill flash — uses crashFlashColor (orange/gold).</summary>
        public void FlashCrash()
        {
            TriggerFlash(crashFlashColor);
        }

        public void SetColor(Color color)
        {
            flashColor = color;
        }

        // ═══════════════════════════════════════════════
        //  FLASH ROUTINE
        // ═══════════════════════════════════════════════

        private void TriggerFlash(Color color)
        {
            if (flashCoroutine != null)
                StopCoroutine(flashCoroutine);

            flashCoroutine = StartCoroutine(FlashRoutine(color));
        }

        private IEnumerator FlashRoutine(Color color)
        {
            float punchDuration = flashDuration * 0.2f;
            float fadeDuration  = flashDuration * 0.8f;

            // Punch in
            float elapsed = 0f;
            while (elapsed < punchDuration)
            {
                elapsed += Time.deltaTime;
                SetEdgeAlpha(color, Mathf.Lerp(0f, peakAlpha, elapsed / punchDuration));
                yield return null;
            }

            // Fade out
            elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                SetEdgeAlpha(color, Mathf.Lerp(peakAlpha, 0f, elapsed / fadeDuration));
                yield return null;
            }

            SetEdgeAlpha(color, 0f);
            flashCoroutine = null;
        }

        private void SetEdgeAlpha(Color color, float alpha)
        {
            Color c = new Color(color.r, color.g, color.b, alpha);
            foreach (Image edge in edges)
                if (edge != null) edge.color = c;
        }
    }
}