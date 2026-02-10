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
        
        private int maxHealth;
        private int currentHealth;

        private bool isBot;
        private bool isDead = false;

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
            
            UpdateHealthBar();
        }

        public void TakeDamage(int amount, GameObject shooter)
        {
            if (!isDead)
            {
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
        }

        private void Die(GameObject shooter)
        {
            if (isDead) return;
            isDead = true;
            
            PointsManager.instance.AddPoint(shooter.tag);
            
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
        
        private void UpdateHealthBar()
        {
            float healthPercent = GetHealthPercent();
            
            healthBarManager?.UpdateAllHealthBars(healthPercent);
            
            OnHealthChanged?.Invoke(healthPercent);
        }
    }
}