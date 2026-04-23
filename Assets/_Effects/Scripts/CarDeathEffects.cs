using System.Collections;
using UnityEngine;

namespace _Cars.Scripts
{
    public class CarDeathEffects : MonoBehaviour
    {
        [Header("Smoke")]
        [SerializeField] private ParticleSystem smokeEffect;
        [SerializeField] private float smokeThreshold  = 0.75f;
        [SerializeField] private float smokeMinEmission = 5f;
        [SerializeField] private float smokeMaxEmission = 40f;

        [Header("Flames")]
        [SerializeField] private GameObject flameEffect;
        [SerializeField] private float flameThreshold = 0.25f;

        [Header("Explosion")]
        [SerializeField] private GameObject explosionPrefab;
        [SerializeField] private float explosionLifetime = 3f;
        [SerializeField] private float explosionScale    = 2f;

        [Tooltip("Optional curve to control how smoke emission scales with health")]
        [SerializeField] private AnimationCurve smokeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Header("Name Tag")]
        [SerializeField] private GameObject nameTagCanvas;

        private bool smokeActive = false;

        // ═══════════════════════════════════════════════
        //  LIFECYCLE
        // ═══════════════════════════════════════════════

        private void Awake()
        {
            InitializeEffects();
        }

        private void InitializeEffects()
        {
            if (smokeEffect != null)
            {
                var emission = smokeEffect.emission;
                emission.enabled      = true;
                emission.rateOverTime = 0f;
            }

            if (flameEffect != null)
                flameEffect.SetActive(false);
        }

        // ═══════════════════════════════════════════════
        //  REWIRING — called by CarVisualLoader after spawn
        // ═══════════════════════════════════════════════

        public void SetReferences(ParticleSystem smoke, GameObject flame, GameObject nameTag)
        {
            smokeEffect   = smoke;
            flameEffect   = flame;
            nameTagCanvas = nameTag;

            InitializeEffects();
            Debug.Log("[CarDeathEffects] References rewired from spawned car prefab.");
        }

        // ═══════════════════════════════════════════════
        //  PUBLIC API
        // ═══════════════════════════════════════════════

        public void OnHealthChanged(float healthPercent)
        {
            if (smokeEffect == null) return;

            if (healthPercent <= smokeThreshold)
            {
                if (!smokeActive) ActivateSmoke();

                float normalized   = Mathf.InverseLerp(smokeThreshold, 0f, healthPercent);
                float curveValue   = smokeCurve.Evaluate(normalized);
                float emissionRate = Mathf.Lerp(smokeMinEmission, smokeMaxEmission, curveValue);

                var emission = smokeEffect.emission;
                emission.rateOverTime = emissionRate;
            }
            else if (smokeActive)
            {
                DeactivateSmoke();
            }

            if (flameEffect != null)
                flameEffect.SetActive(healthPercent <= flameThreshold);
        }

        public void OnDeath()
        {
            SpawnExplosion();
            DeactivateSmoke();

            if (flameEffect   != null) flameEffect.SetActive(false);
            if (nameTagCanvas != null) Destroy(nameTagCanvas);
        }

        // ═══════════════════════════════════════════════
        //  SMOKE
        // ═══════════════════════════════════════════════

        private void ActivateSmoke()
        {
            smokeActive = true;
            smokeEffect.Play();
        }

        private void DeactivateSmoke()
        {
            if (smokeEffect == null) return;
            smokeActive = false;
            smokeEffect.Stop();
            var emission = smokeEffect.emission;
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

            Vector3    spawnPos  = transform.position + Vector3.up * 0.5f;
            GameObject explosion = Instantiate(explosionPrefab, spawnPos, Quaternion.identity);
            explosion.transform.localScale = Vector3.one * explosionScale;
            Destroy(explosion, explosionLifetime);
        }
    }
}