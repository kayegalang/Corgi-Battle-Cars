using UnityEngine;
using UnityEngine.UI;

namespace _UI.Scripts
{
    /// <summary>
    /// Draws black divider lines between player viewports.
    /// Call Setup(playerCount) from SpawnManager after players are spawned.
    ///
    /// 1 player  — no dividers
    /// 2 players — horizontal line (top/bottom split)
    /// 3 players — full cross
    /// 4 players — full cross
    ///
    /// Add to any GameObject in the game scene.
    /// </summary>
    public class SplitScreenDivider : MonoBehaviour
    {
        public static SplitScreenDivider instance { get; private set; }

        [Header("Divider Settings")]
        [Tooltip("Thickness of the divider lines in pixels")]
        [SerializeField] private float lineThickness = 6f;

        [Tooltip("Color of the divider lines")]
        [SerializeField] private Color lineColor = Color.black;

        private Canvas     overlayCanvas;
        private GameObject dividerRoot;

        // ═══════════════════════════════════════════════
        //  LIFECYCLE
        // ═══════════════════════════════════════════════

        private void Awake()
        {
            if (instance != null && instance != this) { Destroy(gameObject); return; }
            instance = this;

            CreateOverlayCanvas();
        }

        // ═══════════════════════════════════════════════
        //  PUBLIC API
        // ═══════════════════════════════════════════════

        /// <summary>
        /// Call this from SpawnManager after players are spawned.
        /// </summary>
        public void Setup(int playerCount)
        {
            BuildDividers(playerCount);
            Debug.Log($"[SplitScreenDivider] Built dividers for {playerCount} player(s).");
        }

        public void SetVisible(bool visible)
        {
            if (dividerRoot != null)
                dividerRoot.SetActive(visible);
        }

        // ═══════════════════════════════════════════════
        //  BUILD
        // ═══════════════════════════════════════════════

        private void BuildDividers(int playerCount)
        {
            if (dividerRoot != null)
                Destroy(dividerRoot);

            if (playerCount <= 1) return;

            dividerRoot = new GameObject("Dividers");
            dividerRoot.transform.SetParent(overlayCanvas.transform, false);

            RectTransform rootRect = dividerRoot.AddComponent<RectTransform>();
            Stretch(rootRect);

            switch (playerCount)
            {
                case 2:
                    CreateVerticalLine(0.5f);
                    break;

                case 3:
                case 4:
                    CreateVerticalLine(0.5f);
                    CreateHorizontalLine(0.5f);
                    break;
            }
        }

        // ═══════════════════════════════════════════════
        //  LINE BUILDERS
        // ═══════════════════════════════════════════════

        private void CreateVerticalLine(float xNormalized)
        {
            GameObject line        = new GameObject("VerticalDivider");
            line.transform.SetParent(dividerRoot.transform, false);

            RectTransform rect    = line.AddComponent<RectTransform>();
            rect.anchorMin        = new Vector2(xNormalized, 0f);
            rect.anchorMax        = new Vector2(xNormalized, 1f);
            rect.pivot            = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta        = new Vector2(lineThickness, 0f);

            AddImage(line);
        }

        private void CreateHorizontalLine(float yNormalized)
        {
            GameObject line        = new GameObject("HorizontalDivider");
            line.transform.SetParent(dividerRoot.transform, false);

            RectTransform rect    = line.AddComponent<RectTransform>();
            rect.anchorMin        = new Vector2(0f, yNormalized);
            rect.anchorMax        = new Vector2(1f, yNormalized);
            rect.pivot            = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta        = new Vector2(0f, lineThickness);

            AddImage(line);
        }

        // ═══════════════════════════════════════════════
        //  HELPERS
        // ═══════════════════════════════════════════════

        private void CreateOverlayCanvas()
        {
            GameObject canvasGo    = new GameObject("DividerCanvas");
            canvasGo.transform.SetParent(transform, false);

            overlayCanvas              = canvasGo.AddComponent<Canvas>();
            overlayCanvas.renderMode   = RenderMode.ScreenSpaceOverlay;
            overlayCanvas.sortingOrder = 99;

            canvasGo.AddComponent<CanvasScaler>();
            canvasGo.AddComponent<GraphicRaycaster>();
        }

        private void AddImage(GameObject go)
        {
            Image img         = go.AddComponent<Image>();
            img.color         = lineColor;
            img.raycastTarget = false;
        }

        private void Stretch(RectTransform rect)
        {
            rect.anchorMin        = Vector2.zero;
            rect.anchorMax        = Vector2.one;
            rect.offsetMin        = Vector2.zero;
            rect.offsetMax        = Vector2.zero;
            rect.anchoredPosition = Vector2.zero;
        }
    }
}