using _Cars.Scripts;
using _Gameplay.Scripts;
using _Projectiles.ScriptableObjects;
using UnityEngine;

namespace _Cars.Scripts
{
    public class WeaponStatsLoader : MonoBehaviour
    {
        [Header("Available Weapon Types")]
        [Tooltip("All weapons available — SAME ORDER as CharacterSelectUI!")]
        [SerializeField] private ProjectileObject[] availableWeaponTypes;

        private static readonly System.Collections.Generic.Dictionary<string, int> TagToIndex
            = new System.Collections.Generic.Dictionary<string, int>
        {
            { "PlayerOne",   0 },
            { "PlayerTwo",   1 },
            { "PlayerThree", 2 },
            { "PlayerFour",  3 },
        };

        private void Awake()
        {
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "DesignerTuning")
                return;

            if (GameplayManager.instance != null &&
                GameplayManager.instance.GetCurrentGameMode() == GameMode.Singleplayer)
                LoadAndApplySelectedWeapon();
        }

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
                Debug.LogError($"[WeaponStatsLoader] No weapon types assigned on {gameObject.name}!");
                return;
            }

            ProjectileObject selected = GetSelectedWeapon();
            if (selected == null) return;

            ApplyToCarShooter(selected);
            Debug.Log($"[WeaponStatsLoader] {gameObject.name} → {selected.ProjectileName}");
        }

        private ProjectileObject GetSelectedWeapon()
        {
            // Try PlayerLoadout first — direct ScriptableObject reference
            int playerIdx = GetPlayerIndex();
            ProjectileObject fromLoadout = PlayerLoadout.GetWeapon(playerIdx);
            if (fromLoadout != null) return fromLoadout;

            // Fall back to PlayerPrefs index
            int saved = PlayerPrefs.GetInt("SelectedWeaponTypeIndex", 0);
            int index = Mathf.Clamp(saved, 0, availableWeaponTypes.Length - 1);
            return availableWeaponTypes[index];
        }

        private int GetPlayerIndex()
        {
            if (TagToIndex.TryGetValue(gameObject.tag, out int index))
                return index;
            return 0;
        }

        private void ApplyToCarShooter(ProjectileObject weapon)
        {
            CarShooter shooter = GetComponent<CarShooter>();
            if (shooter == null) return;
            shooter.SetProjectileType(weapon);
        }

        public void ApplyWeapon(ProjectileObject weapon)
        {
            if (weapon == null) return;
            ApplyToCarShooter(weapon);
        }
    }
}