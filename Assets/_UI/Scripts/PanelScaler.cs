using UnityEngine;

namespace _UI.Scripts
{
    /// <summary>
    /// Scales all children to fit within this panel's rect.
    /// Add to PlayerPanel prefab root to make contents scale
    /// correctly in split screen character select.
    /// </summary>
    [ExecuteAlways]
    public class PanelScaler : MonoBehaviour
    {
        [Tooltip("The resolution this panel was designed for")]
        [SerializeField] private Vector2 referenceResolution = new Vector2(720, 1080);

        private RectTransform rectTransform;
        private Transform contentRoot;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            // The content to scale is the first child
            if (transform.childCount > 0)
                contentRoot = transform.GetChild(0);
        }

        private void Update()
        {
            ApplyScale();
        }

        private void ApplyScale()
        {
            if (rectTransform == null || contentRoot == null) return;

            // Get actual panel size
            float panelWidth  = rectTransform.rect.width;
            float panelHeight = rectTransform.rect.height;

            if (panelWidth <= 0 || panelHeight <= 0) return;

            // Calculate scale to fit reference resolution into panel
            float scaleX = panelWidth  / referenceResolution.x;
            float scaleY = panelHeight / referenceResolution.y;
            float scale  = Mathf.Min(scaleX, scaleY);

            contentRoot.localScale = new Vector3(scale, scale, 1f);
        }
    }
}
