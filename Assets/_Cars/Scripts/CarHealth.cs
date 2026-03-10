using _Bot.Scripts;
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
        
        [Header("Events")]
        public UnityEvent<float> OnHealthChanged;
        
        private HealthBarManager healthBarManager;
        private DeathSpectateManager deathSpectateManager;
        
        private readonly int maxHealth = 100;
        private int currentHealth;
        private bool isBot;
        private bool isDead = false;
        
        private const float RESPAWN_DELAY = 3f;

        void Start()
        {
            isBot = GetComponent<BotAI>() != null;
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
            if (healthBarManager != null)
            {
                healthBarManager.gameObject.SetActive(false);
            }
            
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