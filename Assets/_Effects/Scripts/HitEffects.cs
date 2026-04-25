using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace _Effects.Scripts
{
    public class HitEffects : MonoBehaviour
    {
        [Header("Vignette Flash")]
        [SerializeField] private Volume postProcessVolume;
        [SerializeField] private Color  vignetteHitColor     = new Color(0.8f, 0f, 0f);
        [SerializeField] private float  vignetteHitIntensity = 0.6f;
        [SerializeField] private float  vignetteFadeDuration = 0.3f;

        [Header("Car Flash")]
        [Tooltip("Leave empty — CarVisualLoader assigns renderers after the car model spawns")]
        [SerializeField] private Renderer[] carRenderers;
        [SerializeField] private Color flashColor    = new Color(1f, 0.1f, 0.1f);
        [SerializeField] private float flashDuration = 0.1f;
        [SerializeField] private int   flashCount    = 2;

        private Vignette     vignette;
        private Color        vignetteOriginalColor;
        private float        vignetteOriginalIntensity;

        private Material[][] originalMaterials;
        private Material[][] flashMaterials;

        private Coroutine vignetteCoroutine;
        private Coroutine flashCoroutine;

        // ═══════════════════════════════════════════════
        //  LIFECYCLE
        // ═══════════════════════════════════════════════

        private void Awake()
        {
            SetupVignette();
            // Don't call SetupCarFlash here — car model doesn't exist yet.
            // CarVisualLoader calls SetRenderers() after spawning the car.
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
                Debug.LogWarning($"[HitEffects] No Vignette override found on Volume profile on {gameObject.name}!");
            }
        }

        private void SetupCarFlash()
        {
            if (carRenderers == null || carRenderers.Length == 0) return;

            // Destroy old flash materials to avoid leaks when re-called
            if (flashMaterials != null)
                foreach (var mats in flashMaterials)
                    if (mats != null)
                        foreach (var mat in mats)
                            if (mat != null) Destroy(mat);

            originalMaterials = new Material[carRenderers.Length][];
            flashMaterials    = new Material[carRenderers.Length][];

            for (int i = 0; i < carRenderers.Length; i++)
            {
                if (carRenderers[i] == null) continue;

                originalMaterials[i] = carRenderers[i].materials;
                flashMaterials[i]    = new Material[carRenderers[i].materials.Length];

                for (int j = 0; j < flashMaterials[i].Length; j++)
                {
                    flashMaterials[i][j]       = new Material(carRenderers[i].materials[j]);
                    flashMaterials[i][j].color = flashColor;
                }
            }
        }

        // ═══════════════════════════════════════════════
        //  REWIRING — called by CarVisualLoader after spawn
        // ═══════════════════════════════════════════════

        public void SetRenderers(Renderer[] renderers)
        {
            carRenderers = renderers;
            SetupCarFlash();
            Debug.Log($"[HitEffects] Renderers rewired — {renderers.Length} renderer(s).");
        }

        public void ApplyHueShiftToOriginals(float hue, string propertyName)
        {
            if (originalMaterials == null) return;
            foreach (var mats in originalMaterials)
            {
                if (mats == null) continue;
                foreach (var mat in mats)
                    if (mat != null && mat.HasProperty(propertyName))
                        mat.SetFloat(propertyName, hue);
            }
        }

        // ═══════════════════════════════════════════════
        //  PUBLIC API
        // ═══════════════════════════════════════════════

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

            float targetIntensity = intensity < 0 ? vignetteHitIntensity : intensity;
            float fadeDuration    = duration  < 0 ? vignetteFadeDuration : duration;

            vignette.color.Override(vignetteHitColor);
            vignette.intensity.Override(targetIntensity);

            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                vignette.intensity.Override(
                    Mathf.Lerp(targetIntensity, vignetteOriginalIntensity, elapsed / fadeDuration));
                yield return null;
            }

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
                SetCarMaterials(flash: true);
                yield return new WaitForSeconds(flashDuration);
                SetCarMaterials(flash: false);
                yield return new WaitForSeconds(flashDuration);
            }

            SetCarMaterials(flash: false);
        }

        private void SetCarMaterials(bool flash)
        {
            if (carRenderers == null) return;
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
            if (flashMaterials != null)
                foreach (var mats in flashMaterials)
                    if (mats != null)
                        foreach (var mat in mats)
                            if (mat != null) Destroy(mat);
        }
    }
}