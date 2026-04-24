using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace _Effects.Scripts
{
    /// <summary>
    /// Handles visual hit feedback:
    ///   1. Red vignette flash on the player's screen
    ///   2. Car material flashes red briefly
    /// Add to the player prefab root.
    /// Assign the player's Global Volume and the car's renderers in the Inspector.
    /// </summary>
    public class HitEffects : MonoBehaviour
    {
        [Header("Vignette Flash")]
        [Tooltip("The Global Volume on this player's camera — must have a Vignette override")]
        [SerializeField] private Volume         postProcessVolume;
        [SerializeField] private Color          vignetteHitColor    = new Color(0.8f, 0f, 0f);
        [SerializeField] private float          vignetteHitIntensity = 0.6f;
        [SerializeField] private float          vignetteFadeDuration = 0.3f;

        [Header("Car Flash")]
        [Tooltip("All renderers on the car that should flash red")]
        [SerializeField] private Renderer[]     carRenderers;
        [SerializeField] private Color          flashColor          = new Color(1f, 0.1f, 0.1f);
        [SerializeField] private float          flashDuration       = 0.1f;
        [SerializeField] private int            flashCount          = 2;

        // Runtime
        private Vignette        vignette;
        private Color           vignetteOriginalColor;
        private float           vignetteOriginalIntensity;

        private Material[][]    originalMaterials;  // per renderer, per material
        private Material[][]    flashMaterials;

        private Coroutine       vignetteCoroutine;
        private Coroutine       flashCoroutine;

        // ═══════════════════════════════════════════════
        //  LIFECYCLE
        // ═══════════════════════════════════════════════

        private void Awake()
        {
            SetupVignette();
            SetupCarFlash();
        }

        private void SetupVignette()
        {
            if (postProcessVolume == null) return;

            if (postProcessVolume.profile.TryGet(out vignette))
            {
                vignetteOriginalColor     = vignette.color.value;
                vignetteOriginalIntensity = vignette.intensity.value;
            }
            else
            {
                Debug.LogWarning($"[HitEffects] {gameObject.name} — no Vignette override found on Volume profile! Add one.");
            }
        }

        private void SetupCarFlash()
        {
            if (carRenderers == null || carRenderers.Length == 0)
            {
                // Auto-find renderers if not assigned
                carRenderers = GetComponentsInChildren<Renderer>();
            }

            // Cache original materials and create flash copies
            originalMaterials = new Material[carRenderers.Length][];
            flashMaterials    = new Material[carRenderers.Length][];

            for (int i = 0; i < carRenderers.Length; i++)
            {
                if (carRenderers[i] == null) continue;

                originalMaterials[i] = carRenderers[i].materials;

                // Create flash material copies
                flashMaterials[i] = new Material[carRenderers[i].materials.Length];
                for (int j = 0; j < flashMaterials[i].Length; j++)
                {
                    flashMaterials[i][j] = new Material(carRenderers[i].materials[j]);
                    flashMaterials[i][j].color = flashColor;
                }
            }
        }

        // ═══════════════════════════════════════════════
        //  PUBLIC API
        // ═══════════════════════════════════════════════

        /// <summary>Call from CarHealth.TakeDamage() to trigger all hit effects</summary>
        public void PlayHitEffect()
        {
            if (vignette != null)
            {
                if (vignetteCoroutine != null) StopCoroutine(vignetteCoroutine);
                vignetteCoroutine = StartCoroutine(VignetteFlash());
            }

            if (carRenderers != null && carRenderers.Length > 0)
            {
                if (flashCoroutine != null) StopCoroutine(flashCoroutine);
                flashCoroutine = StartCoroutine(CarFlash());
            }
        }

        /// <summary>Call from CarHealth.Die() for a stronger death flash</summary>
        public void PlayDeathEffect()
        {
            if (vignetteCoroutine != null) StopCoroutine(vignetteCoroutine);
            vignetteCoroutine = StartCoroutine(VignetteFlash(intensity: 0.9f, duration: 0.5f));
        }

        // ═══════════════════════════════════════════════
        //  VIGNETTE FLASH
        // ═══════════════════════════════════════════════

        private IEnumerator VignetteFlash(float intensity = -1f, float duration = -1f)
        {
            if (vignette == null) yield break;

            float targetIntensity = intensity  < 0 ? vignetteHitIntensity  : intensity;
            float fadeDuration    = duration   < 0 ? vignetteFadeDuration  : duration;

            // Snap to hit color
            vignette.color.Override(vignetteHitColor);
            vignette.intensity.Override(targetIntensity);

            // Fade back to original
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / fadeDuration;
                vignette.intensity.Override(Mathf.Lerp(targetIntensity, vignetteOriginalIntensity, t));
                yield return null;
            }

            // Restore
            vignette.color.Override(vignetteOriginalColor);
            vignette.intensity.Override(vignetteOriginalIntensity);
        }

        // ═══════════════════════════════════════════════
        //  CAR FLASH
        // ═══════════════════════════════════════════════

        private IEnumerator CarFlash()
        {
            for (int f = 0; f < flashCount; f++)
            {
                // Flash red
                SetCarMaterials(flash: true);
                yield return new WaitForSeconds(flashDuration);

                // Restore original
                SetCarMaterials(flash: false);
                yield return new WaitForSeconds(flashDuration);
            }

            // Always make sure we end on original materials
            SetCarMaterials(flash: false);
        }

        private void SetCarMaterials(bool flash)
        {
            for (int i = 0; i < carRenderers.Length; i++)
            {
                if (carRenderers[i] == null) continue;
                carRenderers[i].materials = flash ? flashMaterials[i] : originalMaterials[i];
            }
        }

        // ═══════════════════════════════════════════════
        //  CLEANUP
        // ═══════════════════════════════════════════════

        private void OnDestroy()
        {
            // Destroy flash material copies to avoid memory leaks
            if (flashMaterials != null)
                foreach (var mats in flashMaterials)
                    if (mats != null)
                        foreach (var mat in mats)
                            if (mat != null) Destroy(mat);
        }
    }
}
