using System.Collections;
using UnityEngine;

namespace _Cars.Scripts
{
    /// <summary>
    /// Handles visual death effects:
    ///   1. Smoke particle system that activates at low health
    ///   2. Explosion particle system spawned on death
    /// Add to the player prefab root.
    /// </summary>
    public class CarDeathEffects : MonoBehaviour
    {
        [Header("Smoke")]
        [Tooltip("Particle system for low-health smoke — assign a child PS on the car")]
        [SerializeField] private ParticleSystem smokeEffect;

        [Tooltip("Health percentage (0-1) at which smoke starts")]
        [SerializeField] private float smokeThreshold = 0.25f;

        [Tooltip("Emission rate at exactly the smoke threshold")]
        [SerializeField] private float smokeMinEmission = 5f;

        [Tooltip("Emission rate at 0 health (maximum smoke)")]
        [SerializeField] private float smokeMaxEmission = 40f;

        [Header("Explosion")]
        [Tooltip("Explosion particle system prefab — instantiated at death position")]
        [SerializeField] private GameObject explosionPrefab;

        [Tooltip("How long before the explosion GameObject is destroyed")]
        [SerializeField] private float explosionLifetime = 3f;

        [Tooltip("Scale of the explosion — increase for a bigger boom")]
        [SerializeField] private float explosionScale = 2f;

        private bool smokeActive = false;

        // ═══════════════════════════════════════════════
        //  LIFECYCLE
        // ═══════════════════════════════════════════════

        private void Awake()
        {
            // Make sure smoke starts off
            if (smokeEffect != null)
            {
                smokeEffect.Stop();
                var emission = smokeEffect.emission;
                emission.enabled = false;
            }
        }

        // ═══════════════════════════════════════════════
        //  PUBLIC API — called from CarHealth
        // ═══════════════════════════════════════════════

        /// <summary>
        /// Call every time health changes. Handles smoke activation and intensity.
        /// </summary>
        public void OnHealthChanged(float healthPercent)
        {
            Debug.Log($"[CarDeathEffects] OnHealthChanged called: {healthPercent}, smokeEffect={smokeEffect != null}");

            if (smokeEffect == null) return;

            if (healthPercent <= smokeThreshold)
            {
                if (!smokeActive)
                    ActivateSmoke();

                // Scale emission with how low health is
                // 0.25 health = min emission, 0 health = max emission
                float t            = 1f - (healthPercent / smokeThreshold);
                float emissionRate = Mathf.Lerp(smokeMinEmission, smokeMaxEmission, t);

                var emission      = smokeEffect.emission;
                emission.enabled  = true;
                emission.rateOverTime = emissionRate;
            }
            else if (smokeActive)
            {
                DeactivateSmoke();
            }
        }

        /// <summary>
        /// Call from CarHealth.Die() to spawn the explosion.
        /// </summary>
        public void OnDeath()
        {
            Debug.Log($"[CarDeathEffects] OnDeath called! explosionPrefab={explosionPrefab != null}");

            SpawnExplosion();
            DeactivateSmoke();
        }

        // ═══════════════════════════════════════════════
        //  SMOKE
        // ═══════════════════════════════════════════════

        private void ActivateSmoke()
        {
            smokeActive = true;
            smokeEffect.Play();
            Debug.Log($"[CarDeathEffects] {gameObject.name} smoke activated!");
        }

        private void DeactivateSmoke()
        {
            if (smokeEffect == null) return;
            smokeActive = false;
            smokeEffect.Stop();
            var emission     = smokeEffect.emission;
            emission.enabled = false;
        }

        // ═══════════════════════════════════════════════
        //  EXPLOSION
        // ═══════════════════════════════════════════════

        private void SpawnExplosion()
        {
            if (explosionPrefab == null)
            {
                Debug.LogWarning("[CarDeathEffects] No explosion prefab assigned!");
                return;
            }

            // Spawn slightly above the car so it's visible
            Vector3 spawnPos = transform.position + Vector3.up * 0.5f;

            GameObject explosion = Instantiate(
                explosionPrefab,
                spawnPos,
                Quaternion.identity);

            explosion.transform.localScale = Vector3.one * explosionScale;

            // Destroy after particles finish
            Destroy(explosion, explosionLifetime);

            Debug.Log($"[CarDeathEffects] {gameObject.name} exploded!");
        }
    }
}
