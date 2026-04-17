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
        [Tooltip("Particle system to mimic health bar — assign a child PS on the car")]
        [SerializeField] private ParticleSystem smokeEffect;

        [Tooltip("Health percentage (0-1) at which smoke starts")]
        [SerializeField] private float smokeThreshold = 0.75f;

        [Tooltip("Emission rate at exactly the smoke threshold")]
        [SerializeField] private float smokeMinEmission = 5f;

        [Tooltip("Emission rate at 0 health (maximum smoke)")]
        [SerializeField] private float smokeMaxEmission = 40f;

        /*[Header("Spark")]
        [Tooltip("Particle system for low health")]
        [SerializeField] private ParticleSystem sparkEffect;

        [Tooltip("Health percentage (0-1) at which sparks start")]
        [SerializeField] private float sparkThreshold = 0.25f;
        */

        [Header("Flames")]
        [Tooltip("Flame parent object that holds the two flame objects as a child")]
        [SerializeField] private GameObject flameEffect;

        [Tooltip("Health percentage (0-1) at which sparks start")]
        [SerializeField] private float flameThreshold = 0.25f;

        [Header("Explosion")]
        [Tooltip("Explosion particle system prefab — instantiated at death position")]
        [SerializeField] private GameObject explosionPrefab;

        [Tooltip("How long before the explosion GameObject is destroyed")]
        [SerializeField] private float explosionLifetime = 3f;

        [Tooltip("Scale of the explosion — increase for a bigger boom")]
        [SerializeField] private float explosionScale = 2f;

        [Tooltip("Optional curve to control how smoke emission scales with health (0-1)")]
        [SerializeField] private AnimationCurve smokeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Header("Name Tag")]
        [SerializeField] private GameObject nameTagCanvas;

        private bool smokeActive = false;

        // ═══════════════════════════════════════════════
        //  LIFECYCLE
        // ═══════════════════════════════════════════════

        private void Awake()
        {
            if (smokeEffect != null)
            {
                var emission = smokeEffect.emission;
                emission.enabled = true;
                emission.rateOverTime = 0f; //restart the health bar smoke effect with 0 emission, so nothing shows
            }

            if (flameEffect != null)
            {
                flameEffect.SetActive(false);
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
                float normalized = Mathf.InverseLerp(smokeThreshold, 0f, healthPercent);
                float curveValue = smokeCurve.Evaluate(normalized);
                float emissionRate = Mathf.Lerp(smokeMinEmission, smokeMaxEmission, curveValue);

                var emission = smokeEffect.emission;
                emission.rateOverTime = emissionRate;
            }
            else if (smokeActive)
            {
                DeactivateSmoke();
            }

            if (flameEffect != null)
            {
                if (healthPercent <= flameThreshold)
                {
                    if (!flameEffect.activeSelf)
                        flameEffect.SetActive(true);
                }
                else
                {
                    if (flameEffect.activeSelf)
                        flameEffect.SetActive(false);
                }
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

            if (flameEffect != null)
                flameEffect.SetActive(false);

            if (nameTagCanvas != null)
                Destroy(nameTagCanvas);
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
