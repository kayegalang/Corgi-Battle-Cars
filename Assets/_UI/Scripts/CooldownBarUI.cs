using UnityEngine;
using UnityEngine.UI;

namespace _UI.Scripts
{
    public class CooldownBarUI : MonoBehaviour
    {
        [Header("Cooldown Sprites")]
        [Tooltip("0 = full (ready), 5 = empty (blocked)")]
        [SerializeField] private Sprite[] cooldownSprites = new Sprite[6];

        [Header("UI References")]
        [SerializeField] private Image cooldownImage;

        [Header("Background Pulse")]
        [SerializeField] private Image backgroundImage;
        [SerializeField] private float pulseSpeed = 6f;

        private float currentCooldown;
        private float maxCooldown;

        private void Start()
        {
            ValidateComponents();
            SetCooldown(0, 1);
        }

        private void Update()
        {
            if (backgroundImage == null) return;

            bool isOnCooldown = currentCooldown > 0f;

            if (isOnCooldown)
            {
                float pulse = (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f;

                // stronger red pulse while blocked
                Color baseColor = new Color(1f, 0f, 0f, 0.25f);
                Color peakColor = new Color(1f, 0f, 0f, 0.65f);

                backgroundImage.color = Color.Lerp(baseColor, peakColor, pulse);
            }
            else
            {
                backgroundImage.color = Color.clear;
            }
        }

        public void UpdateCooldown(float current, float max)
        {
            currentCooldown = current;
            maxCooldown = max;

            if (cooldownImage == null || cooldownSprites == null || cooldownSprites.Length == 0)
                return;

            float percent = Mathf.Clamp01(current / max);

            int index = Mathf.RoundToInt(percent * (cooldownSprites.Length - 1));
            index = Mathf.Clamp(index, 0, cooldownSprites.Length - 1);

            cooldownImage.sprite = cooldownSprites[index];
        }

        public void SetCooldown(float currentCooldown, float maxCooldown)
        {
            UpdateCooldown(currentCooldown, maxCooldown);
        }

        private void ValidateComponents()
        {
            if (cooldownImage == null)
                Debug.LogError($"[{nameof(CooldownBarUI)}] cooldownImage not assigned!");

            if (backgroundImage == null)
                Debug.LogWarning($"[{nameof(CooldownBarUI)}] backgroundImage not assigned");
        }
    }
}