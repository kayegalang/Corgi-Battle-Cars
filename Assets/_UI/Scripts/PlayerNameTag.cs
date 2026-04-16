using _Bot.Scripts;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _UI.Scripts
{
    /// <summary>
    /// Sets the player name tag label (P1, P2, P3, P4) based on the player's tag.
    /// Hides the name tag entirely for bots.
    /// Add to the player prefab root. Assign the NameTagCanvas root and the PlayerLabel text.
    /// </summary>
    public class PlayerNameTag : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("The root GameObject of the name tag — the whole collar. Gets hidden for bots.")]
        [SerializeField] private GameObject nameTagRoot;

        [Tooltip("The TextMeshPro text showing P1, P2 etc.")]
        [SerializeField] private TextMeshProUGUI playerLabelText;

        [Header("Per-Player Colors")]
        [Tooltip("Tint the collar a different color per player. Leave empty to skip.")]
        [SerializeField] private Image collarImage;

        [Tooltip("Any other images that should also get the transparency applied (tag plate, studs etc.)")]
        [SerializeField] private Image[] additionalImages;

        [SerializeField] private Color playerOneColor   = new Color(0.8f, 0.2f, 0.2f); // red
        [SerializeField] private Color playerTwoColor   = new Color(0.2f, 0.4f, 0.9f); // blue
        [SerializeField] private Color playerThreeColor = new Color(0.2f, 0.7f, 0.2f); // green
        [SerializeField] private Color playerFourColor  = new Color(0.8f, 0.6f, 0.1f); // gold

        [Header("Transparency")]
        [Tooltip("0 = invisible, 1 = fully opaque. Applied to collar and all additional images.")]
        [Range(0f, 1f)]
        [SerializeField] private float nameTagAlpha = 1f;

        // ═══════════════════════════════════════════════
        //  LIFECYCLE
        // ═══════════════════════════════════════════════

        private void Start()
        {
            // Hide entirely for bots — they don't get a name tag
            if (GetComponent<BotAI>() != null)
            {
                if (nameTagRoot != null)
                    nameTagRoot.SetActive(false);
                return;
            }

            SetLabel();
            SetColor();
        }

        // ═══════════════════════════════════════════════
        //  LABEL
        // ═══════════════════════════════════════════════

        private void SetLabel()
        {
            if (playerLabelText == null) return;

            string tag   = gameObject.tag;
            string label = tag.Contains("One")   ? "Player1" :
                           tag.Contains("Two")   ? "Player2" :
                           tag.Contains("Three") ? "Player3" :
                           tag.Contains("Four")  ? "Player4" : "??";

            playerLabelText.text = label;

            // Apply transparency to text too
            if (playerLabelText != null)
            {
                Color textColor = playerLabelText.color;
                textColor.a = nameTagAlpha;
                playerLabelText.color = textColor;
            }
        }

        // ═══════════════════════════════════════════════
        //  COLOR
        // ═══════════════════════════════════════════════

        private void SetColor()
        {
            if (collarImage == null) return;

            string tag = gameObject.tag;

            Color c = tag.Contains("One")   ? playerOneColor   :
                      tag.Contains("Two")   ? playerTwoColor   :
                      tag.Contains("Three") ? playerThreeColor :
                      tag.Contains("Four")  ? playerFourColor  :
                      collarImage.color;

            // Apply global transparency to collar
            c.a = nameTagAlpha;
            collarImage.color = c;

            // Apply same transparency to any additional images (tag plate, studs etc.)
            if (additionalImages != null)
            {
                foreach (Image img in additionalImages)
                {
                    if (img == null) continue;
                    Color imgColor = img.color;
                    imgColor.a = nameTagAlpha;
                    img.color  = imgColor;
                }
            }
        }
    }
}