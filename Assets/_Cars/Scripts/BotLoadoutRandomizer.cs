using _Bot.Scripts;
using _Cars.Scripts;
using _Cars.ScriptableObjects;
using _Projectiles.ScriptableObjects;
using UnityEngine;
using System.Collections.Generic;

namespace _Cars.Scripts
{
    /// <summary>
    /// Randomly assigns a car type and weapon to a bot on spawn.
    /// Loadout is stored statically by bot tag so it stays the same
    /// across the entire game even when the bot respawns.
    /// Add to the bot prefab alongside BotAI.
    /// </summary>
    public class BotLoadoutRandomizer : MonoBehaviour
    {
        [Header("Available Loadouts")]
        [SerializeField] private CarStats[]         availableCarTypes;
        [SerializeField] private ProjectileObject[] availableWeaponTypes;

        // Static storage — survives bot destroy/respawn cycles
        // Key = bot tag (e.g. "BotOne"), Value = (carIndex, weaponIndex)
        private static readonly Dictionary<string, (int carIndex, int weaponIndex)> persistedLoadouts
            = new Dictionary<string, (int, int)>();

        /// <summary>Call this from GameFlowController or SpawnManager when a new match starts.</summary>
        public static void ClearPersistedLoadouts()
        {
            persistedLoadouts.Clear();
            Debug.Log("[BotLoadoutRandomizer] Loadouts cleared for new match.");
        }

        private void Awake()
        {
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "DesignerTuning")
                return;

            // Stats can be applied in Awake with a random pick —
            // we'll finalize by tag in Start() once SpawnManager has set the tag
            _carIndex    = availableCarTypes    != null && availableCarTypes.Length    > 0
                ? Random.Range(0, availableCarTypes.Length)    : 0;
            _weaponIndex = availableWeaponTypes != null && availableWeaponTypes.Length > 0
                ? Random.Range(0, availableWeaponTypes.Length) : 0;

            ApplyCar(_carIndex);
            ApplyWeapon(_weaponIndex);
        }

        private void Start()
        {
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "DesignerTuning")
                return;

            // Tag is now correctly set by SpawnManager — use it as stable key
            string botTag = gameObject.tag;

            if (persistedLoadouts.TryGetValue(botTag, out var existing))
            {
                // Reuse existing loadout on respawn
                _carIndex    = existing.carIndex;
                _weaponIndex = existing.weaponIndex;
                ApplyCar(_carIndex);
                ApplyWeapon(_weaponIndex);
                Debug.Log($"[BotLoadoutRandomizer] {botTag} reusing loadout: car={_carIndex}, weapon={_weaponIndex}");
            }
            else
            {
                // First spawn — store the randomly picked loadout
                persistedLoadouts[botTag] = (_carIndex, _weaponIndex);
                Debug.Log($"[BotLoadoutRandomizer] {botTag} new loadout: car={_carIndex}, weapon={_weaponIndex}");
            }

            ApplyVisuals(_carIndex, _weaponIndex);
        }

        private int _carIndex;
        private int _weaponIndex;

        // ═══════════════════════════════════════════════
        //  APPLY CAR STATS
        // ═══════════════════════════════════════════════

        private void ApplyCar(int carIndex)
        {
            if (availableCarTypes == null || carIndex >= availableCarTypes.Length) return;

            CarStats car = availableCarTypes[carIndex];

            GetComponent<CarStatsLoader>()?.ApplyCarStats(car);
            SetPrivateField(GetComponent<BotController>(), "carStats", car);
            SetPrivateField(GetComponent<CarHealth>(),     "carStats", car);

            Debug.Log($"[BotLoadoutRandomizer] {gameObject.name} → car: {car.CarName}");
        }

        // ═══════════════════════════════════════════════
        //  APPLY WEAPON STATS
        // ═══════════════════════════════════════════════

        private void ApplyWeapon(int weaponIndex)
        {
            if (availableWeaponTypes == null || weaponIndex >= availableWeaponTypes.Length) return;

            ProjectileObject weapon = availableWeaponTypes[weaponIndex];

            GetComponent<BotAI>()?.SetProjectile(weapon);

            Debug.Log($"[BotLoadoutRandomizer] {gameObject.name} → weapon: {weapon.ProjectileName}");
        }

        // ═══════════════════════════════════════════════
        //  APPLY VISUALS
        // ═══════════════════════════════════════════════

        private void ApplyVisuals(int carIndex, int weaponIndex)
        {
            CarVisualLoader loader = GetComponent<CarVisualLoader>();
            if (loader == null) return;

            // Override PlayerPrefs temporarily so CarVisualLoader reads the right indices
            // We use bot-specific keys so we don't overwrite the human player's selection
            string botTag = gameObject.tag;
            PlayerPrefs.SetInt($"BotCarIndex_{botTag}",    carIndex);
            PlayerPrefs.SetInt($"BotWeaponIndex_{botTag}", weaponIndex);

            loader.LoadVisuals(carIndex, weaponIndex);
        }

        // ═══════════════════════════════════════════════
        //  HELPERS
        // ═══════════════════════════════════════════════

        private void SetPrivateField(object target, string fieldName, object value)
        {
            if (target == null) return;
            target.GetType()
                .GetField(fieldName,
                    System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.Instance)
                ?.SetValue(target, value);
        }
    }
}