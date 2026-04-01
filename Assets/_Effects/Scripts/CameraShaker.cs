using System.Collections;
using UnityEngine;

namespace _Cars.Scripts
{
    /// <summary>
    /// Shakes the ShakePivot child of PlayerCamera.
    /// CinemachineBrain only controls PlayerCamera's transform,
    /// so shaking a child is unaffected by Cinemachine.
    ///
    /// Hierarchy setup:
    ///   PlayerCamera          (CinemachineBrain here)
    ///     └── ShakePivot      (Camera component here — shake this)
    ///
    /// Add this script to the player prefab root.
    /// Drag ShakePivot into the Inspector slot.
    /// </summary>
    public class CameraShaker : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Drag the ShakePivot GameObject here (child of PlayerCamera)")]
        [SerializeField] private Transform shakePivot;

        [Header("Shake Strengths")]
        [SerializeField] private float takeDamageShake  = 0.15f;
        [SerializeField] private float deathShake       = 0.4f;
        [SerializeField] private float shootShake       = 0.03f;
        [SerializeField] private float crashShake       = 0.2f;
        [SerializeField] private float landShake        = 0.1f;
        [SerializeField] private float superJumpShake   = 0.12f;
        [SerializeField] private float poopSlipShake    = 0.2f;
        [SerializeField] private float barkHitShake     = 0.18f;
        [SerializeField] private float powerUpShake     = 0.06f;
        [SerializeField] private float countdownGoShake = 0.3f;

        [Header("Shake Settings")]
        [SerializeField] private float shakeDuration  = 0.2f;

        private Coroutine shakeCoroutine;

        // ═══════════════════════════════════════════════
        //  LIFECYCLE
        // ═══════════════════════════════════════════════

        private void Awake()
        {
            if (shakePivot == null)
                Debug.LogError("[CameraShaker] ShakePivot not assigned! Drag it in the Inspector.");
            else
                Debug.Log("[CameraShaker] ShakePivot ready: " + shakePivot.name);
        }

        // ═══════════════════════════════════════════════
        //  PUBLIC SHAKE METHODS
        // ═══════════════════════════════════════════════

        public void ShakeTakeDamage()  => TriggerShake(takeDamageShake,  shakeDuration);
        public void ShakeDeath()       => TriggerShake(deathShake,       shakeDuration * 2f);
        public void ShakeShoot()       => TriggerShake(shootShake,       shakeDuration * 0.5f);
        public void ShakeLand()        => TriggerShake(landShake,        shakeDuration);
        public void ShakeSuperJump()   => TriggerShake(superJumpShake,   shakeDuration);
        public void ShakePoopSlip()    => TriggerShake(poopSlipShake,    shakeDuration);
        public void ShakeBarkHit()     => TriggerShake(barkHitShake,     shakeDuration);
        public void ShakePowerUp()     => TriggerShake(powerUpShake,     shakeDuration * 0.5f);
        public void ShakeCountdownGo() => TriggerShake(countdownGoShake, shakeDuration * 1.5f);

        public void ShakeCrash(float impactSpeed)
        {
            float strength = Mathf.Clamp01(impactSpeed / 20f) * crashShake;
            TriggerShake(strength, shakeDuration);
        }

        // ═══════════════════════════════════════════════
        //  CORE
        // ═══════════════════════════════════════════════

        private void TriggerShake(float strength, float duration)
        {
            if (shakePivot == null) return;

            if (shakeCoroutine != null)
                StopCoroutine(shakeCoroutine);

            shakeCoroutine = StartCoroutine(ShakeCoroutine(strength, duration));
        }

        private IEnumerator ShakeCoroutine(float strength, float duration)
        {
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = 1f - Mathf.Clamp01(elapsed / duration);

                float x = Random.Range(-1f, 1f) * strength * progress;
                float y = Random.Range(-1f, 1f) * strength * progress;

                // Shake local position — parent (PlayerCamera) is controlled by Cinemachine,
                // but our local offset is independent
                shakePivot.localPosition = new Vector3(x, y, 0f);

                yield return null;
            }

            // Always restore to zero when done
            shakePivot.localPosition = Vector3.zero;
            shakeCoroutine = null;
        }
    }
}