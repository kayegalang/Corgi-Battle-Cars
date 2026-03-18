using _Cars.Scripts;
using _Projectiles.ScriptableObjects;
using UnityEngine;

namespace _Cars.Scripts
{
    /// <summary>
    /// Reads the player's selected weapon from CharacterSelect
    /// and applies it to CarShooter at runtime.
    /// Add this to the Player prefab alongside CarStatsLoader!
    /// </summary>
    public class WeaponStatsLoader : MonoBehaviour
    {
        [Header("Available Weapon Types")]
        [Tooltip("All weapons available — SAME ORDER as CharacterSelectUI!")]
        [SerializeField] private ProjectileObject[] availableWeaponTypes;

        private const string SELECTED_INDEX_KEY = "SelectedWeaponTypeIndex";

        private void Awake()
        {
            LoadAndApplySelectedWeapon();
        }

        private void LoadAndApplySelectedWeapon()
        {
            if (availableWeaponTypes == null || availableWeaponTypes.Length == 0)
            {
                Debug.LogError($"[{nameof(WeaponStatsLoader)}] No weapon types assigned on {gameObject.name}!");
                return;
            }

            int savedIndex  = PlayerPrefs.GetInt(SELECTED_INDEX_KEY, 0);
            int index       = Mathf.Clamp(savedIndex, 0, availableWeaponTypes.Length - 1);
            ProjectileObject selected = availableWeaponTypes[index];

            if (selected == null)
            {
                Debug.LogError($"[{nameof(WeaponStatsLoader)}] Weapon at index {index} is null on {gameObject.name}!");
                return;
            }

            ApplyToCarShooter(selected);

            Debug.Log($"[{nameof(WeaponStatsLoader)}] {gameObject.name} loaded weapon: {selected.ProjectileName}");
        }

        private void ApplyToCarShooter(ProjectileObject weapon)
        {
            CarShooter shooter = GetComponent<CarShooter>();

            if (shooter == null)
            {
                Debug.LogWarning($"[{nameof(WeaponStatsLoader)}] No CarShooter found on {gameObject.name}");
                return;
            }

            shooter.SetProjectileType(weapon);
        }

        /// <summary>
        /// Apply a new weapon at runtime (e.g. from TuningManager).
        /// </summary>
        public void ApplyWeapon(ProjectileObject weapon)
        {
            if (weapon == null) return;
            ApplyToCarShooter(weapon);
        }
    }
}