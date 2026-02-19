using System.Collections;
using _Bot.Scripts;
using _PowerUps.ScriptableObjects;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _PowerUps.Scripts
{
    /// <summary>
    /// Sits on the player. Receives power-ups from PowerUpPickup and manages
    /// activating, ticking, and removing effects.
    /// </summary>
    public class PowerUpHandler : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject powerUpUI;          // Panel shown when power-up is active
        [SerializeField] private Image powerUpIcon;             // Icon of the active power-up
        [SerializeField] private TextMeshProUGUI powerUpNameText;
        [SerializeField] private Image timerBar;                // Drains left to right as time runs out
        
        [Header("Bot Settings")]
        [Tooltip("How long after collecting before a bot auto-uses an active power-up (bark/squirrel)")]
        [SerializeField] private float botAutoUseDelay = 1.5f;
        
        // Active power-up state
        private PowerUpObject activePowerUp;
        private float remainingDuration;
        private bool isPowerUpActive = false;
        
        // Super Bark state
        private int barkCharges = 0;
        private SuperBarkPowerUp superBarkData;
        
        // Squirrel state
        private bool hasThrowable = false;
        private SquirrelPowerUp squirrelData;
        
        // Is this handler on a bot?
        public bool IsBot { get; private set; }
        
        private void Awake()
        {
            IsBot = GetComponent<BotAI>() != null;
            HidePowerUpUI();
        }
        
        private void Update()
        {
            if (!isPowerUpActive) return;
            
            TickActivePowerUp();
            UpdateTimerBar();
            
            // Per-frame effect (e.g. poop dropping)
            activePowerUp?.OnUpdate(gameObject);
        }
        
        // ──────────────────────────────────────────────
        //  ACTIVATION
        // ──────────────────────────────────────────────
        
        public void ActivatePowerUp(PowerUpObject powerUp)
        {
            // If a power-up is already active, remove it first
            if (isPowerUpActive)
            {
                RemoveActivePowerUp();
            }
            
            activePowerUp = powerUp;
            remainingDuration = powerUp.duration;
            isPowerUpActive = true;
            
            // Apply the effect
            powerUp.Apply(gameObject);
            
            // Show UI (only meaningful for players but harmless on bots)
            ShowPowerUpUI(powerUp);
            
            // Instant power-ups (duration = 0) remove themselves immediately
            if (powerUp.duration <= 0f)
            {
                RemoveActivePowerUp();
                return;
            }
            
            // Bots auto-use active abilities after a short delay
            if (IsBot)
            {
                StartCoroutine(BotAutoUse());
            }
            
            Debug.Log($"[PowerUpHandler] {gameObject.name} activated {powerUp.powerUpName}!");
        }
        
        /// <summary>
        /// Bots can't press buttons, so after a short delay they auto-use
        /// any power-up that requires manual activation (bark, squirrel).
        /// </summary>
        private IEnumerator BotAutoUse()
        {
            yield return new WaitForSeconds(botAutoUseDelay);
            
            // Auto-bark if they have charges
            if (barkCharges > 0)
            {
                // Keep barking until all charges are spent
                while (barkCharges > 0)
                {
                    UseBark();
                    yield return new WaitForSeconds(0.8f);
                }
                yield break;
            }
            
            // Auto-throw squirrel toward nearest player
            if (hasThrowable && squirrelData != null)
            {
                ThrowSquirrelAtNearestTarget();
            }
        }
        
        private void ThrowSquirrelAtNearestTarget()
        {
            // Find the nearest non-bot target
            float closestDist = Mathf.Infinity;
            GameObject closestTarget = null;
            
            PowerUpHandler[] allHandlers = FindObjectsByType<PowerUpHandler>(FindObjectsSortMode.None);
            foreach (PowerUpHandler handler in allHandlers)
            {
                if (handler.gameObject == gameObject) continue;
                if (handler.IsBot) continue; // Bots throw at players, not other bots
                
                float dist = Vector3.Distance(transform.position, handler.transform.position);
                if (dist < closestDist)
                {
                    closestDist = dist;
                    closestTarget = handler.gameObject;
                }
            }
            
            Vector3 throwDir = closestTarget != null
                ? (closestTarget.transform.position - transform.position).normalized
                : transform.forward;
            
            squirrelData.ThrowSquirrel(gameObject, throwDir);
            hasThrowable = false;
            RemoveActivePowerUp();
        }
        
        private void TickActivePowerUp()
        {
            remainingDuration -= Time.deltaTime;
            
            if (remainingDuration <= 0f)
            {
                RemoveActivePowerUp();
            }
        }
        
        private void RemoveActivePowerUp()
        {
            if (activePowerUp == null) return;
            
            activePowerUp.Remove(gameObject);
            
            Debug.Log($"[PowerUpHandler] {gameObject.name}'s {activePowerUp.powerUpName} expired!");
            
            activePowerUp = null;
            isPowerUpActive = false;
            remainingDuration = 0f;
            
            HidePowerUpUI();
        }
        
        // ──────────────────────────────────────────────
        //  UI
        // ──────────────────────────────────────────────
        
        private void ShowPowerUpUI(PowerUpObject powerUp)
        {
            if (powerUpUI != null) powerUpUI.SetActive(true);
            if (powerUpIcon != null) powerUpIcon.sprite = powerUp.icon;
            if (powerUpNameText != null) powerUpNameText.text = powerUp.powerUpName;
            if (timerBar != null) timerBar.fillAmount = 1f;
        }
        
        private void HidePowerUpUI()
        {
            if (powerUpUI != null) powerUpUI.SetActive(false);
        }
        
        private void UpdateTimerBar()
        {
            if (timerBar == null || activePowerUp == null) return;
            
            timerBar.fillAmount = remainingDuration / activePowerUp.duration;
        }
        
        // ──────────────────────────────────────────────
        //  SUPER BARK
        // ──────────────────────────────────────────────
        
        public void SetBarkCharges(int charges)
        {
            barkCharges = charges;
        }
        
        public void SetSuperBarkData(SuperBarkPowerUp data)
        {
            superBarkData = data;
        }
        
        /// <summary>
        /// Called by player input to use a bark charge.
        /// </summary>
        public void UseBark()
        {
            if (barkCharges <= 0 || superBarkData == null) return;
            
            superBarkData.ExecuteBark(gameObject);
            barkCharges--;
            
            Debug.Log($"[PowerUpHandler] {gameObject.name} barked! {barkCharges} charges left");
            
            // If out of charges, end the power-up
            if (barkCharges <= 0)
            {
                RemoveActivePowerUp();
            }
        }
        
        public int GetBarkCharges() => barkCharges;
        
        // ──────────────────────────────────────────────
        //  SQUIRREL
        // ──────────────────────────────────────────────
        
        public void SetSquirrelData(SquirrelPowerUp data)
        {
            squirrelData = data;
        }
        
        public void SetHasThrowable(bool value)
        {
            hasThrowable = value;
        }
        
        /// <summary>
        /// Called by player input to throw the squirrel.
        /// </summary>
        public void ThrowSquirrel()
        {
            if (!hasThrowable || squirrelData == null) return;
            
            squirrelData.ThrowSquirrel(gameObject, transform.forward);
            
            hasThrowable = false;
            RemoveActivePowerUp();
            
            Debug.Log($"[PowerUpHandler] {gameObject.name} threw the squirrel!");
        }
        
        public bool HasThrowable() => hasThrowable;
        
        // ──────────────────────────────────────────────
        //  GETTERS
        // ──────────────────────────────────────────────
        
        public bool IsPowerUpActive() => isPowerUpActive;
        public PowerUpObject GetActivePowerUp() => activePowerUp;
        public float GetRemainingDuration() => remainingDuration;
    }
}