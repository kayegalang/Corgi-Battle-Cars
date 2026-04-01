using _Bot.Scripts;
using _Cars.ScriptableObjects;
using _Effects.Scripts;
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

        [Header("Spawn Protection")]
        [SerializeField] private float spawnProtectionDuration = 3f;
        [SerializeField] private float flashSpeed = 8f;
        
        [Header("Events")]
        public UnityEvent<float> OnHealthChanged;
        
        private HealthBarManager     healthBarManager;
        private DeathSpectateManager deathSpectateManager;
        
        [Header("Effects")]
        [SerializeField] CameraShaker cameraShaker;
        [SerializeField] HitEffects hitEffects;
        
        private int  maxHealth;
        private int  currentHealth;
        private bool isBot;
        private bool isDead = false;

        // Spawn protection
        private float      spawnProtectionTimer = 0f;
        private bool       isSpawnProtected     = false;
        private Renderer[] carRenderers;

        private const float RESPAWN_DELAY       = 3f;
        private const int   FALLBACK_MAX_HEALTH = 100;

        // ═══════════════════════════════════════════════
        //  LIFECYCLE
        // ═══════════════════════════════════════════════

        private void Start()
        {
            isBot         = GetComponent<BotAI>() != null;
            maxHealth     = DetermineMaxHealth();
            currentHealth = maxHealth;
            
            if (spawnManager == null)
                spawnManager = FindFirstObjectByType<SpawnManager>();
            
            healthBarManager = GetComponentInChildren<HealthBarManager>();

            if (healthBarManager == null)
                Debug.LogWarning($"{gameObject.name}: No HealthBarManager found!");
            
            if (!isBot)
            {
                deathSpectateManager = GetComponent<DeathSpectateManager>();
                
                if (deathSpectateManager == null)
                    Debug.LogWarning($"{gameObject.name}: No DeathSpectateManager found! Player will respawn immediately.");
            }
            
            ValidateCarStats();
            UpdateHealthBar();

            // Cache all renderers for flashing effect
            carRenderers = GetComponentsInChildren<Renderer>();

            // NOTE: No ActivateSpawnProtection() here — only called on respawn by SpawnManager
        }

        private void Update()
        {
            if (!isSpawnProtected) return;

            spawnProtectionTimer -= Time.deltaTime;

            if (spawnProtectionTimer <= 0f)
            {
                DeactivateSpawnProtection();
                return;
            }

            // Flash the car by toggling renderer visibility
            bool visible = Mathf.Sin(Time.time * flashSpeed) > 0f;
            SetRenderersVisible(visible);
        }

        // ═══════════════════════════════════════════════
        //  SPAWN PROTECTION
        // ═══════════════════════════════════════════════

        /// <summary>
        /// Called by SpawnManager after a respawn (not on initial spawn).
        /// </summary>
        public void ActivateSpawnProtection()
        {
            spawnProtectionTimer = spawnProtectionDuration;
            isSpawnProtected     = true;
        }

        private void DeactivateSpawnProtection()
        {
            isSpawnProtected = false;
            SetRenderersVisible(true);
        }

        private void SetRenderersVisible(bool visible)
        {
            if (carRenderers == null) return;
            foreach (Renderer r in carRenderers)
                if (r != null) r.enabled = visible;
        }

        // ═══════════════════════════════════════════════
        //  HEALTH
        // ═══════════════════════════════════════════════

        private int DetermineMaxHealth()
        {
            if (carStats != null)
                return carStats.MaxHealth;
            
            Debug.LogWarning($"{gameObject.name}: No CarStats assigned! Using default health of {FALLBACK_MAX_HEALTH}");
            return FALLBACK_MAX_HEALTH;
        }

        private void ValidateCarStats()
        {
            if (carStats == null)
                Debug.LogWarning($"{gameObject.name}: No CarStats assigned!");
        }

        public void TakeDamage(int amount, GameObject shooter)
        {
            if (isDead) return;

            // Block all damage during spawn protection
            if (isSpawnProtected)
            {
                return;
            }
            
            currentHealth -= amount;
            UpdateHealthBar();

            // Hit feedback
            Debug.Log($"[CarHealth] TakeDamage called on {gameObject.name} — cameraShaker={cameraShaker != null}");
            cameraShaker?.ShakeTakeDamage();
            hitEffects?.PlayHitEffect();

            if (currentHealth <= 0)
                Die(shooter);

            if (isBot && shooter != null)
                GetComponent<BotAI>()?.OnHit(shooter.transform);
        }

        // ═══════════════════════════════════════════════
        //  DEATH
        // ═══════════════════════════════════════════════

        private void Die(GameObject shooter)
        {
            if (isDead) return;
            
            isDead = true;

            // Death feedback
            cameraShaker?.ShakeDeath();
            hitEffects?.PlayDeathEffect();

            if (shooter != null)
                PointsManager.instance.AddPoint(shooter.tag);
            else
                Debug.LogWarning($"[{nameof(CarHealth)}] {gameObject.name} died but shooter was already destroyed!");
            
            if (isBot)
                HandleBotDeath();
            else
                HandlePlayerDeath();
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

        // ═══════════════════════════════════════════════
        //  HEALTH BAR
        // ═══════════════════════════════════════════════

        private void UpdateHealthBar()
        {
            float healthPercent = GetHealthPercent();
            healthBarManager?.UpdateAllHealthBars(healthPercent);
            OnHealthChanged?.Invoke(healthPercent);
        }

        // ═══════════════════════════════════════════════
        //  GETTERS
        // ═══════════════════════════════════════════════

        public float GetHealthPercent()  => (float)currentHealth / maxHealth;
        public bool  GetIsBot()          => isBot;
        public int   GetMaxHealth()      => maxHealth;
        public int   GetCurrentHealth()  => currentHealth;
        public bool  IsDead()            => isDead;
    }
}