using UnityEngine;
using UnityEngine.UI;

namespace _UI.Scripts
{
    /// <summary>
    /// Adds a colored border around a player's split-screen viewport.
    /// Add to each player's PlayerUICanvas GameObject.
    ///
    /// The border is built from 4 thin UI panels (top, bottom, left, right)
    /// anchored to the edges of the canvas.
    ///
    /// Colors match per-player identity — assign in Inspector or
    /// call SetColor() at runtime.
    /// </summary>
    [RequireComponent(typeof(Canvas))]
    public class PlayerScreenBorder : MonoBehaviour
    {
        [Header("Border Settings")]
        [Tooltip("Thickness of the border in pixels")]
        [SerializeField] private float borderThickness = 6f;

        [Tooltip("Color of the border — should match the player's collar color")]
        [SerializeField] private Color borderColor = Color.white;

        [Tooltip("Border opacity")]
        [SerializeField] [Range(0f, 1f)] private float borderAlpha = 0.85f;

        // ─── generated UI ───────────────────────────────
        private GameObject borderRoot;

        // ═══════════════════════════════════════════════
        //  LIFECYCLE
        // ═══════════════════════════════════════════════

        private void Start()
        {
            BuildBorder();
        }

        // ═══════════════════════════════════════════════
        //  BUILD
        // ═══════════════════════════════════════════════

        private void BuildBorder()
        {
            // Root container — stretch to fill the canvas
            borderRoot                     = new GameObject("ScreenBorder");
            borderRoot.transform.SetParent(transform, false);
            borderRoot.transform.SetAsLastSibling(); // render on top

            RectTransform rootRect = borderRoot.AddComponent<RectTransform>();
            StretchToFill(rootRect);

            Color c = new Color(borderColor.r, borderColor.g, borderColor.b, borderAlpha);

            // Build each edge
            CreateEdge(borderRoot.transform, "Top",
                anchorMin: new Vector2(0, 1),
                anchorMax: new Vector2(1, 1),
                pivot:     new Vector2(0.5f, 1f),
                size:      new Vector2(0, borderThickness),
                color:     c);

            CreateEdge(borderRoot.transform, "Bottom",
                anchorMin: new Vector2(0, 0),
                anchorMax: new Vector2(1, 0),
                pivot:     new Vector2(0.5f, 0f),
                size:      new Vector2(0, borderThickness),
                color:     c);

            CreateEdge(borderRoot.transform, "Left",
                anchorMin: new Vector2(0, 0),
                anchorMax: new Vector2(0, 1),
                pivot:     new Vector2(0f, 0.5f),
                size:      new Vector2(borderThickness, 0),
                color:     c);

            CreateEdge(borderRoot.transform, "Right",
                anchorMin: new Vector2(1, 0),
                anchorMax: new Vector2(1, 1),
                pivot:     new Vector2(1f, 0.5f),
                size:      new Vector2(borderThickness, 0),
                color:     c);
        }

        private void CreateEdge(Transform parent, string edgeName,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
            Vector2 size, Color color)
        {
            GameObject edge = new GameObject(edgeName);
            edge.transform.SetParent(parent, false);

            RectTransform rect  = edge.AddComponent<RectTransform>();
            rect.anchorMin      = anchorMin;
            rect.anchorMax      = anchorMax;
            rect.pivot          = pivot;
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta      = size;

            Image img   = edge.AddComponent<Image>();
            img.color   = color;
            img.raycastTarget = false;
        }

        private void StretchToFill(RectTransform rect)
        {
            rect.anchorMin        = Vector2.zero;
            rect.anchorMax        = Vector2.one;
            rect.offsetMin        = Vector2.zero;
            rect.offsetMax        = Vector2.zero;
            rect.anchoredPosition = Vector2.zero;
        }

        // ═══════════════════════════════════════════════
        //  PUBLIC API
        // ═══════════════════════════════════════════════

        /// <summary>
        /// Call this to change the border color at runtime.
        /// e.g. PlayerScreenBorder.SetColor(Color.red) for PlayerOne
        /// </summary>
        public void SetColor(Color color)
        {
            borderColor = color;

            if (borderRoot == null) return;

            Color c = new Color(color.r, color.g, color.b, borderAlpha);
            foreach (Image img in borderRoot.GetComponentsInChildren<Image>())
                img.color = c;
        }

        public void SetVisible(bool visible)
        {
            if (borderRoot != null)
                borderRoot.SetActive(visible);
        }
    }
}
