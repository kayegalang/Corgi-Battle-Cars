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

        public event System.Action<GameObject, GameObject> OnDeath;

        private HealthBarManager healthBarManager;
        private DeathSpectateManager deathSpectateManager;
        private CameraShaker cameraShaker;
        private ControllerRumbler controllerRumbler;
        private HitEffects hitEffects;
        private CarDeathEffects deathEffects;
        private GameObject nameTagCanvas;

        private int maxHealth;
        private int currentHealth;
        private bool isBot;
        private bool isDead = false;

        private float spawnProtectionTimer = 0f;
        private bool isSpawnProtected = false;
        private Renderer[] carRenderers;

        private const float RESPAWN_DELAY = 3f;
        private const int FALLBACK_MAX_HEALTH = 100;

        // ═══════════════════════════════════════════════
        //  START
        // ═══════════════════════════════════════════════

        private void Start()
        {
            isBot = GetComponent<BotAI>() != null;
            maxHealth = DetermineMaxHealth();
            currentHealth = maxHealth;

            if (spawnManager == null)
                spawnManager = FindFirstObjectByType<SpawnManager>();

            healthBarManager = GetComponentInChildren<HealthBarManager>();
            cameraShaker = GetComponent<CameraShaker>();
            controllerRumbler = GetComponent<ControllerRumbler>();
            hitEffects = GetComponent<HitEffects>();
            deathEffects = GetComponent<CarDeathEffects>();

            Transform nameTagTransform = FindDeepChild(transform, "NameTagCanvas");
            if (nameTagTransform != null)
                nameTagCanvas = nameTagTransform.gameObject;

            if (!isBot)
                deathSpectateManager = GetComponent<DeathSpectateManager>();

            carRenderers = GetComponentsInChildren<Renderer>();

            UpdateHealthBar();
        }

        // ═══════════════════════════════════════════════
        //  UPDATE
        // ═══════════════════════════════════════════════

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
        //  DAMAGE SYSTEM
        // ═══════════════════════════════════════════════

        public void TakeDamage(int amount, GameObject shooter)
        {
            TakeDamageInternal(amount, shooter, false);
        }

        public void TakeCrashDamage(int amount, GameObject crasher)
        {
            TakeDamageInternal(amount, crasher, true);
        }

        private void TakeDamageInternal(int amount, GameObject shooter, bool isCrash)
        {
            if (isDead) return;
            if (isSpawnProtected) return;

            currentHealth -= amount;
            UpdateHealthBar();

            cameraShaker?.ShakeTakeDamage();
            controllerRumbler?.RumbleTakeDamage();
            hitEffects?.PlayHitEffect();

            if (currentHealth <= 0)
                Die(shooter, isCrash);
        }

        // ═══════════════════════════════════════════════
        //  DEATH
        // ═══════════════════════════════════════════════

        private void Die(GameObject shooter, bool isCrash)
        {
            if (isDead) return;
            isDead = true;

            OnDeath?.Invoke(gameObject, shooter);

            cameraShaker?.ShakeDeath();
            controllerRumbler?.RumbleDeath();
            hitEffects?.PlayDeathEffect();
            deathEffects?.OnDeath();

            if (isBot)
            {
                HandleBotDeath();
            }
            else
            {
                HandlePlayerDeath(shooter);
            }
        }

        private void HandleBotDeath()
        {
            spawnManager?.Respawn(gameObject.tag);
            Destroy(gameObject);
        }

        private void HandlePlayerDeath(GameObject shooter)
        {
            StartCoroutine(DelayedPlayerDeath(shooter));
        }

        private IEnumerator DelayedPlayerDeath(GameObject shooter)
        {
            SetRenderersVisible(false);

            yield return new WaitForSeconds(0.8f);

            transform.position = new Vector3(0, -1000f, 0);

            if (healthBarManager != null)
                healthBarManager.gameObject.SetActive(false);

            string killerName = shooter != null ? shooter.tag : "Unknown";

            if (deathSpectateManager != null)
            {
                deathSpectateManager.OnPlayerDeath(
                    gameObject.tag,
                    RESPAWN_DELAY,
                    RespawnPlayer,
                    killerName
                );
            }
            else
            {
                HandleBotDeath();
            }
        }

        private void RespawnPlayer()
        {
            spawnManager?.Respawn(gameObject.tag);
            Destroy(gameObject);
        }

        // ═══════════════════════════════════════════════
        //  SPAWN PROTECTION (FIXED API)
        // ═══════════════════════════════════════════════

        public void ActivateSpawnProtection()
        {
            spawnProtectionTimer = spawnProtectionDuration;
            isSpawnProtected = true;
        }

        private void DeactivateSpawnProtection()
        {
            isSpawnProtected = false;
            SetRenderersVisible(true);
        }

        // ═══════════════════════════════════════════════
        //  HEALTH
        // ═══════════════════════════════════════════════

        private void UpdateHealthBar()
        {
            float healthPercent = (float)currentHealth / maxHealth;
            healthBarManager?.UpdateAllHealthBars(healthPercent);
            OnHealthChanged?.Invoke(healthPercent);
            deathEffects?.OnHealthChanged(healthPercent);
        }

        private int DetermineMaxHealth()
        {
            return carStats != null ? carStats.MaxHealth : FALLBACK_MAX_HEALTH;
        }

        // ═══════════════════════════════════════════════
        //  VISUALS
        // ═══════════════════════════════════════════════

        private void SetRenderersVisible(bool visible)
        {
            foreach (Renderer r in carRenderers)
                if (r != null) r.enabled = visible;

            if (nameTagCanvas != null)
                nameTagCanvas.SetActive(visible);
        }

        // ═══════════════════════════════════════════════
        //  HELPERS
        // ═══════════════════════════════════════════════

        private Transform FindDeepChild(Transform parent, string name)
        {
            foreach (Transform child in parent)
            {
                if (child.name == name) return child;

                Transform found = FindDeepChild(child, name);
                if (found != null) return found;
            }
            return null;
        }

        // ═══════════════════════════════════════════════
        //  REQUIRED PUBLIC API (FIXES ALL ERRORS)
        // ═══════════════════════════════════════════════

        public bool IsDead()
        {
            return isDead;
        }
    }
}