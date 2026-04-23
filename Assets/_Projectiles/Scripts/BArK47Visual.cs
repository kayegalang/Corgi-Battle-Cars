using UnityEngine;

namespace _Cars.Scripts
{
    /// <summary>
    /// Weapon visual for bArK-47.
    /// Turret rotates to aim, barrel spins while firing.
    /// Add to the bArK-47 weapon prefab root.
    /// </summary>
    public class BArK47Visual : WeaponVisualBase
    {
        [Header("bArK-47 Barrel")]
        [SerializeField] [Tooltip("The barrel child that spins while firing")]
        private Transform barrelModel;

        [SerializeField] [Tooltip("Degrees per second the barrel spins while firing")]
        private float barrelSpinSpeed = 720f;

        [SerializeField] [Tooltip("How fast the barrel decelerates when not firing")]
        private float barrelDeceleration = 360f;

        private float currentBarrelSpin   = 0f;
        private float currentSpinVelocity = 0f;

        // ═══════════════════════════════════════════════
        //  OVERRIDES
        // ═══════════════════════════════════════════════

        protected override void OnFireStart()
        {
            currentSpinVelocity = barrelSpinSpeed;
        }

        protected override void OnFireStop()
        {
            // Velocity decelerates naturally in OnIdleTick
        }

        protected override void OnFiringTick()
        {
            currentSpinVelocity = barrelSpinSpeed;
            SpinBarrel();
        }

        protected override void OnIdleTick()
        {
            currentSpinVelocity = Mathf.MoveTowards(
                currentSpinVelocity, 0f, barrelDeceleration * Time.deltaTime);
            SpinBarrel();
        }

        // ═══════════════════════════════════════════════
        //  BARREL
        // ═══════════════════════════════════════════════

        private void SpinBarrel()
        {
            if (barrelModel == null) return;
            currentBarrelSpin         += currentSpinVelocity * Time.deltaTime;
            barrelModel.localRotation = Quaternion.Euler(0f, currentBarrelSpin, 0f);
        }
    }
}