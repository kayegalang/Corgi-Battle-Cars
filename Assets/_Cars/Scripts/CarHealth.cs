using System.Collections;
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

        /// <summary>
        /// Fired when this car dies. Parameters: (victim, killer).
        /// </summary>
        public event System.Action<GameObject, GameObject> OnDeath;
        
        private HealthBarManager     healthBarManager;
        private DeathSpectateManager deathSpectateManager;
        private CameraShaker         cameraShaker;
        private ControllerRumbler    controllerRumbler;
        private HitEffects           hitEffects;
        private CarDeathEffects      deathEffects;
        private GameObject           nameTagCanvas;
        
        private int  maxHealth;
        private int  currentHealth;
        private bool isBot;
        private bool isDead = false;

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
            
            healthBarManager  = GetComponentInChildren<HealthBarManager>();
            cameraShaker      = GetComponent<CameraShaker>();
            controllerRumbler = GetComponent<ControllerRumbler>();
            hitEffects        = GetComponent<HitEffects>();
            deathEffects      = GetComponent<CarDeathEffects>();

            // Search entire hierarchy for NameTagCanvas regardless of depth
            Transform nameTagTransform = transform.Find("Visuals/CarModel/NameTagCanvas");
            if (nameTagTransform != null)
                nameTagCanvas = nameTagTransform.gameObject;
            else
                Debug.LogWarning($"[CarHealth] NameTagCanvas not found on {gameObject.name} — license plate won't flash during spawn protection!");

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

            carRenderers = GetComponentsInChildren<Renderer>();
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

            bool visible = Mathf.Sin(Time.time * flashSpeed) > 0f;
            SetRenderersVisible(visible);
        }

        // ═══════════════════════════════════════════════
        //  SPAWN PROTECTION
        // ═══════════════════════════════════════════════

        public void ActivateSpawnProtection()
        {
            spawnProtectionTimer = spawnProtectionDuration;
            isSpawnProtected     = true;
            Debug.Log($"[CarHealth] {gameObject.name} spawn protected for {spawnProtectionDuration}s!");
        }

        private void DeactivateSpawnProtection()
        {
            isSpawnProtected = false;
            SetRenderersVisible(true);
            Debug.Log($"[CarHealth] {gameObject.name} spawn protection ended!");
        }

        private void SetRenderersVisible(bool visible)
        {
            if (carRenderers != null)
                foreach (Renderer r in carRenderers)
                    if (r != null) r.enabled = visible;

            if (nameTagCanvas != null)
                nameTagCanvas.SetActive(visible);
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
            else
                Debug.Log($"[CarHealth] {gameObject.name} using CarStats: {carStats.name} (Max Health: {maxHealth})");
        }

        public void TakeDamage(int amount, GameObject shooter)
        {
            if (isDead) return;

            if (isSpawnProtected)
            {
                Debug.Log($"[CarHealth] {gameObject.name} is spawn protected — damage blocked!");
                return;
            }
            
            currentHealth -= amount;
            UpdateHealthBar();

            cameraShaker?.ShakeTakeDamage();
            controllerRumbler?.RumbleTakeDamage();
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

            OnDeath?.Invoke(gameObject, shooter);

            cameraShaker?.ShakeDeath();
            controllerRumbler?.RumbleDeath();
            hitEffects?.PlayDeathEffect();
            deathEffects?.OnDeath();

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
            StartCoroutine(DelayedPlayerDeath());
        }

        private IEnumerator DelayedPlayerDeath()
        {
            // Hide the car immediately on death
            SetRenderersVisible(false);

            yield return new WaitForSeconds(0.8f);

            // Move below the map so physics doesn't interfere
            transform.position = new Vector3(0, -1000f, 0);

            // Keep renderers HIDDEN — no need to re-enable since
            // this GameObject is about to be destroyed on respawn.
            // Re-enabling here caused a flash at the death position.

            if (healthBarManager != null)
                healthBarManager.gameObject.SetActive(false);

            if (deathSpectateManager != null)
                deathSpectateManager.OnPlayerDeath(gameObject.tag, RESPAWN_DELAY, RespawnPlayer);
            else
            {
                Debug.LogWarning($"[{nameof(CarHealth)}] No DeathSpectateManager found!");
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
            deathEffects?.OnHealthChanged(healthPercent);
        }

        // ═══════════════════════════════════════════════
        //  HELPERS
        // ═══════════════════════════════════════════════

        private Transform FindDeepChild(Transform parent, string childName)
        {
            foreach (Transform child in parent)
            {
                if (child.name == childName) return child;
                Transform found = FindDeepChild(child, childName);
                if (found != null) return found;
            }
            return null;
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