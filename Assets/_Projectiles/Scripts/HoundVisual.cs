using UnityEngine;
using _Projectiles.ScriptableObjects;

namespace _Cars.Scripts
{
    /// <summary>
    /// Weapon visual for The Hound.
    /// Turret rotates to aim on Y axis.
    /// Laser beam activates while firing and deals damage via raycast.
    /// Add to The Hound weapon prefab root.
    ///
    /// PREFAB STRUCTURE:
    ///   The Hound (root) ← HoundVisual here
    ///     ├── Plasma Gun Base     ← static, doesn't rotate
    ///     ├── TurretPivot         ← assign as Turret Root, rotates to aim
    ///     │     └── [swivel mesh]
    ///     └── FirePoint
    ///           └── LaserBeam     ← LaserBeamVisual + Line Renderer here
    ///                 ├── ChargeEffect
    ///                 └── ImpactEffect
    /// </summary>
    public class HoundVisual : WeaponVisualBase
    {
        [Header("The Hound Laser")]
        [SerializeField] private LaserBeamVisual laserBeam;
        [SerializeField] private ParticleSystem  chargeEffect;

        [Header("Damage (mirrors ProjectileObject values)")]
        [Tooltip("Damage dealt per second while the laser is on target")]
        [SerializeField] private int   damagePerSecond  = 20;
        [Tooltip("How often damage is applied in seconds")]
        [SerializeField] private float damageInterval   = 0.1f;

        private GameObject ownerRoot;

        // ═══════════════════════════════════════════════
        //  SETUP
        // ═══════════════════════════════════════════════

        private void Awake()
        {
            if (laserBeam != null)
                laserBeam.SetActive(false);

            if (chargeEffect != null)
                chargeEffect.Stop();
        }

        /// <summary>Called by CarVisualLoader to pass the player root for damage attribution.</summary>
        public void SetOwner(GameObject owner)
        {
            ownerRoot = owner;

            CarShooter shooter = owner.GetComponent<CarShooter>();

            if (laserBeam != null)
                laserBeam.SetDamageSource(owner, shooter, damagePerSecond, damageInterval);
        }

        // ═══════════════════════════════════════════════
        //  OVERRIDES
        // ═══════════════════════════════════════════════

        protected override void OnFireStart()
        {
            if (laserBeam    != null) laserBeam.SetActive(true);
            if (chargeEffect != null) chargeEffect.Play();
        }

        protected override void OnFireStop()
        {
            if (laserBeam    != null) laserBeam.SetActive(false);
            if (chargeEffect != null) chargeEffect.Stop();
        }
    }
}