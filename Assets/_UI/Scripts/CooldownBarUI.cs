using UnityEngine;
using UnityEngine.UI;

namespace _UI.Scripts
{
    public class CooldownBarUI : MonoBehaviour
    {
        [Header("UI References")]
        [Tooltip("The Image component that fills (should be set to Filled type)")]
        [SerializeField] private Image cooldownFillImage;
        
        [Header("Visual Settings")]
        [Tooltip("Color when weapon is ready to fire (bar is full)")]
        [SerializeField] private Color weaponReadyColor = new Color(0.2f, 0.6f, 1f, 1f);
        
        [Tooltip("Color when weapon is on cooldown (bar is empty/filling)")]
        [SerializeField] private Color onCooldownColor = new Color(1f, 0.2f, 0.2f, 1f);
        
        [Header("Optional Background")]
        [Tooltip("Background image (optional - can be null)")]
        [SerializeField] private Image backgroundImage;
        
        private void Start()
        {
            ValidateComponents();
            InitializeFillImage();
            SetWeaponReady();
        }
        
        private void ValidateComponents()
        {
            if (cooldownFillImage == null)
            {
                Debug.LogError($"[{nameof(CooldownBarUI)}] Cooldown fill image not assigned!");
            }
        }
        
        private void InitializeFillImage()
        {
            if (cooldownFillImage == null)
            {
                return;
            }
            
            cooldownFillImage.type = Image.Type.Filled;
            cooldownFillImage.fillMethod = Image.FillMethod.Horizontal;
        }
        
        public void UpdateCooldown(float currentCooldown, float maxCooldown)
        {
            if (cooldownFillImage == null)
            {
                Debug.LogError($"[{nameof(CooldownBarUI)}] {gameObject.name} - cooldownFillImage is NULL!");
                return;
            }
            
            float fillAmount = CalculateFillAmount(currentCooldown, maxCooldown);
            
            SetFillAmount(fillAmount); // This now handles color gradient!
        }
        
        private float CalculateFillAmount(float current, float max)
        {
            if (max <= 0f)
            {
                return 1f;
            }
            
            return Mathf.Clamp01(current / max);
        }
        
        private void SetFillAmount(float amount)
        {
            cooldownFillImage.fillAmount = amount;
            
            // Smooth color gradient based on fill amount
            // 100% - 50% = Blue to Yellow
            // 50% - 0% = Yellow to Red
            Color fillColor;
            
            if (amount >= 0.5f)
            {
                // High charge: Lerp from Yellow (0.5) to Blue (1.0)
                float t = (amount - 0.5f) / 0.5f; // 0 at 50%, 1 at 100%
                fillColor = Color.Lerp(Color.yellow, weaponReadyColor, t);
            }
            else
            {
                // Low charge: Lerp from Red (0) to Yellow (0.5)
                float t = amount / 0.5f; // 0 at 0%, 1 at 50%
                fillColor = Color.Lerp(onCooldownColor, Color.yellow, t);
            }
            
            cooldownFillImage.color = fillColor;
        }
        
        private bool WeaponIsReady(float fillAmount)
        {
            return fillAmount >= 1f;
        }
        
        private void SetOnCooldown()
        {
            if (cooldownFillImage != null)
            {
                cooldownFillImage.color = onCooldownColor;
            }
        }
        
        private void SetWeaponReady()
        {
            if (cooldownFillImage != null)
            {
                cooldownFillImage.color = weaponReadyColor;
                cooldownFillImage.fillAmount = 1f;
            }
        }
        
        public void Hide()
        {
            gameObject.SetActive(false);
        }
        
        public void Show()
        {
            gameObject.SetActive(true);
        }
    }
}