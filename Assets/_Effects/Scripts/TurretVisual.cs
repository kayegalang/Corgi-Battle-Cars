using UnityEngine;
using _Bot.Scripts;

namespace _Cars.Scripts
{
    /// <summary>
    /// Drives weapon visuals by finding the WeaponVisualBase component
    /// on the spawned weapon model and calling Tick() every frame.
    ///
    /// All weapon-specific logic (barrel spin, laser, etc.) lives in
    /// subclasses of WeaponVisualBase on each weapon prefab.
    ///
    /// Add to the player/bot prefab root.
    /// CarVisualLoader calls SetWeaponVisual() after spawning the weapon.
    /// </summary>
    public class TurretVisuals : MonoBehaviour
    {
        private CarShooter      carShooter;
        private BotAI           botAI;
        private WeaponVisualBase weaponVisual;

        // ═══════════════════════════════════════════════
        //  LIFECYCLE
        // ═══════════════════════════════════════════════

        private void Awake()
        {
            carShooter = GetComponent<CarShooter>();
            botAI      = GetComponent<BotAI>();

            if (carShooter == null && botAI == null)
                Debug.LogError($"[TurretVisuals] No CarShooter or BotAI found on {gameObject.name}!");
        }

        private void Update()
        {
            if (weaponVisual == null) return;
            weaponVisual.Tick(GetAimDirection(), GetIsFiring());
        }

        // ═══════════════════════════════════════════════
        //  REWIRING — called by CarVisualLoader after spawn
        // ═══════════════════════════════════════════════

        public void SetWeaponVisual(WeaponVisualBase visual)
        {
            weaponVisual = visual;
            Debug.Log($"[TurretVisuals] Weapon visual set: {visual.GetType().Name}");
        }

        // ═══════════════════════════════════════════════
        //  HELPERS
        // ═══════════════════════════════════════════════

        private Vector3 GetAimDirection()
        {
            if (carShooter != null) return carShooter.GetAimDirection();
            if (botAI      != null) return botAI.GetAimDirection();
            return Vector3.zero;
        }

        private bool GetIsFiring()
        {
            if (carShooter != null) return carShooter.IsFiring();
            if (botAI      != null) return botAI.IsFiring();
            return false;
        }
    }
}