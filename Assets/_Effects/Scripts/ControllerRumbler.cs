using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace _Effects.Scripts
{
    /// <summary>
    /// Gamepad rumble feedback — mirrors the CameraShaker pattern.
    /// Add to the player prefab root alongside CameraShaker.
    /// Silently does nothing if no gamepad is connected (keyboard players).
    ///
    /// CALL SITES:
    ///   CarShooter   → RumbleShoot()
    ///   CarHealth    → RumbleTakeDamage(), RumbleDeath()
    ///   CarController → RumbleCrash(speed), RumbleLand(), RumblePoopSlip()
    ///                   RumbleZoomiesStart(), RumbleZoomiesStop()
    ///   PowerUpHandler → RumbleCollectPowerUp()
    /// </summary>
    public class ControllerRumbler : MonoBehaviour
    {
        [Header("Shoot")]
        [SerializeField] private float shootLow      = 0.0f;
        [SerializeField] private float shootHigh     = 0.6f;
        [SerializeField] private float shootDuration = 0.08f;

        [Header("Take Damage")]
        [SerializeField] private float damageLow      = 0.5f;
        [SerializeField] private float damageHigh     = 0.8f;
        [SerializeField] private float damageDuration = 0.25f;

        [Header("Death")]
        [SerializeField] private float deathLow      = 1.0f;
        [SerializeField] private float deathHigh     = 1.0f;
        [SerializeField] private float deathDuration = 0.8f;

        [Header("Crash")]
        [Tooltip("Max rumble strength at full crash speed")]
        [SerializeField] private float crashMaxStrength = 0.9f;
        [Tooltip("Speed at which crash rumble reaches max strength")]
        [SerializeField] private float crashMaxSpeed    = 20f;
        [SerializeField] private float crashDuration    = 0.2f;

        [Header("Land")]
        [SerializeField] private float landLow      = 0.2f;
        [SerializeField] private float landHigh     = 0.3f;
        [SerializeField] private float landDuration = 0.1f;

        [Header("Poop Slip")]
        [SerializeField] private float slipLow      = 0.3f;
        [SerializeField] private float slipHigh     = 0.5f;
        [SerializeField] private float slipDuration = 1.5f;
        [Tooltip("How fast the slip rumble oscillates")]
        [SerializeField] private float slipFrequency = 8f;

        [Header("Collect Power-Up")]
        [SerializeField] private float collectLow      = 0.0f;
        [SerializeField] private float collectHigh     = 1.0f;
        [SerializeField] private float collectDuration = 0.15f;

        [Header("Zoomies")]
        [SerializeField] private float zoomiesLow  = 0.15f;
        [SerializeField] private float zoomiesHigh = 0.05f;

        // ─── Runtime state ───────────────────────────
        private PlayerInput playerInput;
        private Gamepad     gamepad;
        private Coroutine   activeRumble;
        private bool        zoomiesActive = false;

        // ═══════════════════════════════════════════════
        //  LIFECYCLE
        // ═══════════════════════════════════════════════

        private void Awake()
        {
            playerInput = GetComponent<PlayerInput>();
        }

        private void Start()
        {
            RefreshGamepad();
        }

        private void OnDisable()
        {
            StopRumble();
        }

        private void OnDestroy()
        {
            StopRumble();
        }

        // ═══════════════════════════════════════════════
        //  PUBLIC API — mirrors CameraShaker method names
        // ═══════════════════════════════════════════════

        /// <summary>Called by CarShooter each time a shot fires.</summary>
        public void RumbleShoot()
        {
            TriggerRumble(shootLow, shootHigh, shootDuration);
        }

        /// <summary>Called by CarHealth.TakeDamage().</summary>
        public void RumbleTakeDamage()
        {
            TriggerRumble(damageLow, damageHigh, damageDuration);
        }

        /// <summary>Called by CarHealth.Die().</summary>
        public void RumbleDeath()
        {
            TriggerRumble(deathLow, deathHigh, deathDuration);
        }

        /// <summary>Called by CarController.OnCollisionEnter() — scales with impact speed.</summary>
        public void RumbleCrash(float impactSpeed)
        {
            float t        = Mathf.Clamp01(impactSpeed / crashMaxSpeed);
            float strength = t * crashMaxStrength;
            TriggerRumble(strength * 0.5f, strength, crashDuration);
        }

        /// <summary>Called by CarController when landing from a jump.</summary>
        public void RumbleLand()
        {
            TriggerRumble(landLow, landHigh, landDuration);
        }

        /// <summary>Called by CarController.TriggerSlip() for poop power-up.</summary>
        public void RumblePoopSlip()
        {
            if (activeRumble != null) StopCoroutine(activeRumble);
            activeRumble = StartCoroutine(SlipRumble());
        }

        /// <summary>Called by PowerUpHandler when a power-up is collected.</summary>
        public void RumbleCollectPowerUp()
        {
            TriggerRumble(collectLow, collectHigh, collectDuration);
        }

        /// <summary>Called by CarController.ApplySpeedMultiplier() when zoomies start.</summary>
        public void RumbleZoomiesStart()
        {
            zoomiesActive = true;
            RefreshGamepad();
            if (gamepad != null)
                gamepad.SetMotorSpeeds(zoomiesLow, zoomiesHigh);
        }

        /// <summary>Called by CarController.RemoveSpeedMultiplier() when zoomies end.</summary>
        public void RumbleZoomiesStop()
        {
            zoomiesActive = false;
            StopRumble();
        }

        // ═══════════════════════════════════════════════
        //  INTERNAL
        // ═══════════════════════════════════════════════

        private void TriggerRumble(float low, float high, float duration)
        {
            if (zoomiesActive) return; // don't interrupt continuous zoomies rumble

            RefreshGamepad();
            if (gamepad == null) return;

            if (activeRumble != null) StopCoroutine(activeRumble);
            activeRumble = StartCoroutine(TimedRumble(low, high, duration));
        }

        private IEnumerator TimedRumble(float low, float high, float duration)
        {
            RefreshGamepad();
            if (gamepad == null) yield break;

            gamepad.SetMotorSpeeds(low, high);
            yield return new WaitForSeconds(duration);
            StopRumble();
        }

        private IEnumerator SlipRumble()
        {
            RefreshGamepad();
            if (gamepad == null) yield break;

            float elapsed = 0f;
            while (elapsed < slipDuration)
            {
                elapsed += Time.deltaTime;
                float t       = elapsed / slipDuration;
                float fade    = 1f - t; // fades out as slip ends
                float wave    = Mathf.Abs(Mathf.Sin(elapsed * slipFrequency * Mathf.PI));
                float low     = slipLow  * wave * fade;
                float high    = slipHigh * wave * fade;

                gamepad?.SetMotorSpeeds(low, high);
                yield return null;
            }

            StopRumble();
        }

        private void StopRumble()
        {
            RefreshGamepad();
            gamepad?.SetMotorSpeeds(0f, 0f);
        }

        private void RefreshGamepad()
        {
            if (playerInput == null) return;

            foreach (var device in playerInput.devices)
            {
                if (device is Gamepad gp)
                {
                    gamepad = gp;
                    return;
                }
            }

            gamepad = null; // keyboard player — no rumble
        }
    }
}