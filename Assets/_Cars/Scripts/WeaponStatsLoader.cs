using _Cars.Scripts;
using _Gameplay.Scripts;
using _Projectiles.ScriptableObjects;
using UnityEngine;

namespace _Cars.Scripts
{
    /// <summary>
    /// Reads the player's selected weapon from CharacterSelect and applies it to CarShooter.
    /// In multiplayer, SpawnManager calls LoadForCurrentTag() after setting the tag.
    /// </summary>
    public class WeaponStatsLoader : MonoBehaviour
    {
        [Header("Available Weapon Types")]
        [Tooltip("All weapons available — SAME ORDER as CharacterSelectUI!")]
        [SerializeField] private ProjectileObject[] availableWeaponTypes;

        private const string BASE_KEY = "SelectedWeaponTypeIndex";

        private void Awake()
        {
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "DesignerTuning")
                return;

            // In singleplayer the tag is already correct on the prefab so load immediately
            // In multiplayer SpawnManager calls LoadForCurrentTag() after setting the tag
            if (GameplayManager.instance != null &&
                GameplayManager.instance.GetCurrentGameMode() == GameMode.Singleplayer)
                LoadAndApplySelectedWeapon();
        }

        /// <summary>
        /// Called by SpawnManager after SetPlayerIdentity() so the tag is correct.
        /// </summary>
        public void LoadForCurrentTag()
        {
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "DesignerTuning")
                return;

            LoadAndApplySelectedWeapon();
        }

        private void LoadAndApplySelectedWeapon()
        {
            if (availableWeaponTypes == null || availableWeaponTypes.Length == 0)
            {
                Debug.LogError($"[{nameof(WeaponStatsLoader)}] No weapon types assigned on {gameObject.name}!");
                return;
            }

            string           key      = GetPlayerPrefsKey();
            int              saved    = PlayerPrefs.GetInt(key, 0);
            int              index    = Mathf.Clamp(saved, 0, availableWeaponTypes.Length - 1);
            ProjectileObject selected = availableWeaponTypes[index];

            if (selected == null)
            {
                Debug.LogError($"[{nameof(WeaponStatsLoader)}] Weapon at index {index} is null on {gameObject.name}!");
                return;
            }

            ApplyToCarShooter(selected);
            Debug.Log($"[{nameof(WeaponStatsLoader)}] {gameObject.name} loaded weapon: {selected.ProjectileName} (key: {key})");
        }

        private string GetPlayerPrefsKey()
        {
            // Singleplayer — use base key (saved by CharacterSelectUI)
            if (GameplayManager.instance != null &&
                GameplayManager.instance.GetCurrentGameMode() == GameMode.Singleplayer)
                return BASE_KEY;

            // Multiplayer — per-player key based on tag
            string tag = gameObject.tag;
            if (tag == "PlayerOne"   || tag == "PlayerTwo" ||
                tag == "PlayerThree" || tag == "PlayerFour")
                return $"{BASE_KEY}_{tag}";

            return BASE_KEY;
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