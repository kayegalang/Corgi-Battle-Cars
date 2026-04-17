using UnityEngine;
using UnityEngine.UI;

namespace _UI.Scripts
{
    public class CooldownBarUI : MonoBehaviour
    {
        [Header("Cooldown Sprites")]
        [Tooltip("0 = full bar, 5 = empty bar")]
        [SerializeField] private Sprite[] cooldownSprites = new Sprite[6];

        [Header("UI References")]
        [Tooltip("Image component to display the current cooldown sprite")]
        [SerializeField] private Image cooldownImage;

        private void Start()
        {
            ValidateComponents();
            SetCooldown(0, 0); // start full
        }

        public void UpdateCooldown(float current, float max)
        {
            if (cooldownImage == null || cooldownSprites == null || cooldownSprites.Length == 0)
                return;

            float percent = Mathf.Clamp01(current / max); // 1 = full, 0 = empty

            int index = Mathf.FloorToInt((cooldownSprites.Length - 1) * (1f - percent));

            cooldownImage.sprite = cooldownSprites[index];
        }

        private void ValidateComponents()
        {
            if (cooldownImage == null)
            {
                Debug.LogError($"[{nameof(CooldownBarUI)}] cooldownImage not assigned!");
            }

            if (cooldownSprites == null || cooldownSprites.Length != 6)
            {
                Debug.LogError($"[{nameof(CooldownBarUI)}] cooldownSprites array must have 6 elements (0-5)!");
            }
        }

        /// <summary>
        /// Call this from your weapon/ability system
        /// currentCooldown: 0 = ready, maxCooldown = full cooldown
        /// </summary>
        public void SetCooldown(float currentCooldown, float maxCooldown)
        {
            if (cooldownImage == null || cooldownSprites == null || cooldownSprites.Length != 6) return;

            int spriteIndex = Mathf.RoundToInt((currentCooldown / maxCooldown) * 5f); 
            spriteIndex = Mathf.Clamp(spriteIndex, 0, 5);

            // Since 0 = full bar, 5 = empty bar
            cooldownImage.sprite = cooldownSprites[spriteIndex];
        }

        /// <summary>
        /// Shortcut to immediately show full bar
        /// </summary>
        public void SetCooldownFull()
        {
            if (cooldownImage != null)
                cooldownImage.sprite = cooldownSprites[0];
        }

        /// <summary>
        /// Shortcut to immediately show empty bar
        /// </summary>
        public void SetCooldownEmpty()
        {
            if (cooldownImage != null)
                cooldownImage.sprite = cooldownSprites[5];
        }
    }
}