using _Bot.Scripts;
using _Cars.ScriptableObjects;
using _Gameplay.Scripts;
using _UI.Scripts;
using UnityEngine;
using UnityEngine.Events;

namespace _Cars.Scripts
{
    public class CarHealth : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private SpawnManager spawnManager;
        [SerializeField] private CarStats carStats;
        
        [Header("Events")]
        public UnityEvent<float> OnHealthChanged;
        
        private HealthBarManager healthBarManager;
        private DeathSpectateManager deathSpectateManager;
        
        private int maxHealth;
        private int currentHealth;

        private bool isBot;
        private bool isDead = false;
        
        private const float RESPAWN_DELAY = 3f;

        void Start()
        {
            isBot = GetComponent<BotAI>() != null;
            
            maxHealth = (carStats != null) ? carStats.MaxHealth : 100;
            currentHealth = maxHealth;
            
            if (spawnManager == null)
            {
                spawnManager = FindFirstObjectByType<SpawnManager>();
            }
            
            healthBarManager = GetComponentInChildren<HealthBarManager>();
            
            if (healthBarManager == null)
            {
                Debug.LogWarning($"{gameObject.name}: No HealthBarManager found!");
            }
            
            if (carStats == null)
            {
                Debug.LogWarning($"{gameObject.name}: No CarStats assigned! Using default health of 100");
            }
            
            // Get death spectate manager (only for players, not bots)
            if (!isBot)
            {
                deathSpectateManager = GetComponent<DeathSpectateManager>();
            }
            
            UpdateHealthBar();
        }

        public void TakeDamage(int amount, GameObject shooter)
        {
            if (isDead)
            {
                return;
            }
            
            currentHealth -= amount;
            UpdateHealthBar();
    
            if (currentHealth <= 0)
            {
                Die(shooter);
            }

            if (isBot)
            {
                GetComponent<BotAI>()?.OnHit(shooter.transform);
            }
        }

        private void Die(GameObject shooter)
        {
            if (isDead) 
            {
                return;
            }
            
            isDead = true;
            
            // Award point to shooter
            if (PointsManager.instance != null)
            {
                PointsManager.instance.AddPoint(shooter.tag);
            }
            
            // Handle death based on whether this is a bot or player
            if (isBot)
            {
                // Bots get destroyed immediately and respawned
                HandleBotDeath();
            }
            else
            {
                // Players get spectate screen
                HandlePlayerDeath();
            }
        }
        
        private void HandleBotDeath()
        {
            spawnManager?.Respawn(gameObject.tag);
            Destroy(gameObject);
        }
        
        private void HandlePlayerDeath()
        {
            // Hide health bar
            if (healthBarManager != null)
            {
                healthBarManager.gameObject.SetActive(false);
            }
            
            // Show death/spectate screen
            if (deathSpectateManager != null)
            {
                deathSpectateManager.OnPlayerDeath(gameObject.tag, RESPAWN_DELAY);
            }
            else
            {
                Debug.LogWarning($"[{nameof(CarHealth)}] No DeathSpectateManager found! Falling back to immediate respawn");
                HandleBotDeath();
                return;
            }
            
            // Schedule respawn after delay
            Invoke(nameof(RespawnPlayer), RESPAWN_DELAY);
        }
        
        private void RespawnPlayer()
        {
            spawnManager?.Respawn(gameObject.tag);
            Destroy(gameObject);
        }

        public float GetHealthPercent()
        {
            return (float)currentHealth / maxHealth;
        }

        public bool GetIsBot()
        {
            return isBot;
        }
        
        public int GetMaxHealth()
        {
            return maxHealth;
        }
        
        public int GetCurrentHealth()
        {
            return currentHealth;
        }
        
        public bool IsDead()
        {
            return isDead;
        }
        
        private void UpdateHealthBar()
        {
            float healthPercent = GetHealthPercent();
            
            healthBarManager?.UpdateAllHealthBars(healthPercent);
            
            OnHealthChanged?.Invoke(healthPercent);
        }
    }
}