using UnityEngine;
using UnityEngine.InputSystem;

namespace _UI.Scripts
{
    /// <summary>
    /// Add to any panel that needs controller back navigation.
    /// Pressing B (East) hides this panel and shows the previous one.
    /// </summary>
    public class ControllerBackButton : MonoBehaviour
    {
        [Tooltip("The panel to show when going back")]
        [SerializeField] private GameObject previousPanel;

        [Tooltip("Optional — additional panel to hide when going back (if different from this GameObject)")]
        [SerializeField] private GameObject panelToHide;

        private void Update()
        {
            if (Gamepad.current == null) return;
            if (!Gamepad.current.buttonEast.wasPressedThisFrame) return;

            GoBack();
        }

        public void GoBack()
        {
            // Hide this panel (or the specified override)
            GameObject toHide = panelToHide != null ? panelToHide : gameObject;
            toHide.SetActive(false);

            // Show the previous panel
            if (previousPanel != null)
                previousPanel.SetActive(true);
            else
                Debug.LogWarning($"[ControllerBackButton] No previous panel assigned on {gameObject.name}!");
        }
    }
}