using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace _Prototyping.Scripts
{
    /// <summary>
    /// Prevents Unity's ScrollRect from auto-scrolling to whichever
    /// UI element gets selected when clicked (sliders, dropdowns, etc).
    /// Add this to the same GameObject as your ScrollRect.
    /// </summary>
    [RequireComponent(typeof(ScrollRect))]
    public class ScrollRectFocusFix : MonoBehaviour
    {
        private ScrollRect scrollRect;

        private void Awake()
        {
            scrollRect = GetComponent<ScrollRect>();
        }

        private void LateUpdate()
        {
            if (EventSystem.current == null) return;

            // While mouse button is held we leave selection alone (allows slider dragging).
            // Once released, clear selection so ScrollRect has nothing to scroll toward.
            if (!Input.GetMouseButton(0))
            {
                var selected = EventSystem.current.currentSelectedGameObject;
                if (selected != null && IsInsideContent(selected.transform))
                {
                    EventSystem.current.SetSelectedGameObject(null);
                }
            }
        }

        private bool IsInsideContent(Transform child)
        {
            if (scrollRect.content == null) return false;

            Transform t = child;
            while (t != null)
            {
                if (t == scrollRect.content) return true;
                t = t.parent;
            }
            return false;
        }
    }
}
