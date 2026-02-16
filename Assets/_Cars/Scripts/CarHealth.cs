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
        private Rigidbody carRb;
        
        private int maxHealth;
        private int currentHealth;

        private bool isBot;
        private bool isDead = false;
        
        private const float RESPAWN_DELAY = 3f;
        
        private static readonly Vector3 OUT_OF_BOUNDS_POSITION = new Vector3(0f, -500f, 0f);

        void Start()
        {
            isBot = GetComponent<BotAI>() != null;
            carRb = GetComponent<Rigidbody>();
            
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
            
            if (PointsManager.instance != null)
            {
                PointsManager.instance.AddPoint(shooter.tag);
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
            spawnManager?.RespawnBot(gameObject.tag);
            Destroy(gameObject);
        }
        
        private void HandlePlayerDeath()
        {
            HideHealthBar();
            
            TeleportOutOfBounds();
            
            if (deathSpectateManager != null)
            {
                deathSpectateManager.OnPlayerDeath(gameObject.tag, RESPAWN_DELAY, RespawnPlayer);
            }
            else
            {
                Debug.LogWarning($"[{nameof(CarHealth)}] No DeathSpectateManager found! Respawning immediately.");
                RespawnPlayer();
            }
        }

        private void TeleportOutOfBounds()
        {
            if (carRb != null)
            {
                carRb.linearVelocity = Vector3.zero;
                carRb.angularVelocity = Vector3.zero;
                carRb.position = OUT_OF_BOUNDS_POSITION;
            }
            else
            {
                transform.position = OUT_OF_BOUNDS_POSITION;
            }
        }
        
        private void RespawnPlayer()
        {
            if (spawnManager == null)
            {
                Debug.LogError($"[{nameof(CarHealth)}] Cannot respawn — SpawnManager is null!");
                return;
            }
            
            Transform spawnPoint = spawnManager.GetRespawnPoint();
            
            if (spawnPoint == null)
            {
                Debug.LogError($"[{nameof(CarHealth)}] Cannot respawn — no spawn point available!");
                return;
            }
            
            if (carRb != null)
            {
                carRb.linearVelocity = Vector3.zero;
                carRb.angularVelocity = Vector3.zero;
                carRb.position = spawnPoint.position;
                carRb.rotation = spawnPoint.rotation;
            }
            else
            {
                transform.SetPositionAndRotation(spawnPoint.position, spawnPoint.rotation);
            }
            
            isDead = false;
            currentHealth = maxHealth;
            UpdateHealthBar();
            
            ShowHealthBar();
            
            RestorePlayerControls();
        }

        private void RestorePlayerControls()
        {
            CarController carController = GetComponent<CarController>();
            if (carController != null)
                carController.enabled = true;

            CarShooter carShooter = GetComponent<CarShooter>();
            if (carShooter != null)
            {
                carShooter.enabled = true;     
                carShooter.EnableGameplay();   
            }
        }

        private void HideHealthBar()
        {
            if (healthBarManager != null)
                healthBarManager.gameObject.SetActive(false);
        }

        private void ShowHealthBar()
        {
            if (healthBarManager != null)
                healthBarManager.gameObject.SetActive(true);
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