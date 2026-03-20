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
        [Header("Configuration")]
        [SerializeField] private CarStats carStats;
        
        [Header("References")]
        [SerializeField] private SpawnManager spawnManager;
        
        [Header("Events")]
        public UnityEvent<float> OnHealthChanged;
        
        private HealthBarManager healthBarManager;
        private DeathSpectateManager deathSpectateManager;
        
        private int maxHealth;
        private int currentHealth;
        private bool isBot;
        private bool isDead = false;
        
        private const float RESPAWN_DELAY = 3f;
        private const int FALLBACK_MAX_HEALTH = 100;

        void Start()
        {
            isBot = GetComponent<BotAI>() != null;
            maxHealth = DetermineMaxHealth();
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
            
            if (!isBot)
            {
                deathSpectateManager = GetComponent<DeathSpectateManager>();
                
                if (deathSpectateManager == null)
                {
                    Debug.LogWarning($"{gameObject.name}: No DeathSpectateManager found! Player will respawn immediately.");
                }
            }
            
            ValidateCarStats();
            UpdateHealthBar();
        }

        private int DetermineMaxHealth()
        {
            if (carStats != null)
            {
                return carStats.MaxHealth;
            }
            
            Debug.LogWarning($"{gameObject.name}: No CarStats assigned! Using default health of {FALLBACK_MAX_HEALTH}");
            return FALLBACK_MAX_HEALTH;
        }

        private void ValidateCarStats()
        {
            if (carStats == null)
            {
                Debug.LogWarning($"{gameObject.name}: No CarStats assigned!");
            }
            else
            {
                Debug.Log($"[CarHealth] {gameObject.name} using CarStats: {carStats.name} (Max Health: {maxHealth})");
            }
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

            if (isBot && shooter != null)
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
            
            if (shooter != null)
            {
                PointsManager.instance.AddPoint(shooter.tag);
            }
            else
            {
                Debug.LogWarning($"[{nameof(CarHealth)}] {gameObject.name} died but shooter was already destroyed!");
            }
            
            if (isBot)
            {
                HandleBotDeath();
            }
            else
            {
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
            // Teleport far below the map so other players can't shoot the dead car
            transform.position = new Vector3(0, -1000f, 0);

            if (healthBarManager != null)
                healthBarManager.gameObject.SetActive(false);
    
            if (deathSpectateManager != null)
            {
                deathSpectateManager.OnPlayerDeath(gameObject.tag, RESPAWN_DELAY, RespawnPlayer);
            }
            else
            {
                Debug.LogWarning($"[{nameof(CarHealth)}] No DeathSpectateManager found! Falling back to immediate respawn");
                HandleBotDeath();
            }
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