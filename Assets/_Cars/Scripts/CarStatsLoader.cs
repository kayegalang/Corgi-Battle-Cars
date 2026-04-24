using _Cars.ScriptableObjects;
using _Gameplay.Scripts;
using UnityEngine;

namespace _Cars.Scripts
{
    /// <summary>
    /// Loads the player's selected car type from CharacterSelect and applies stats.
    /// In multiplayer, SpawnManager calls LoadForCurrentTag() after setting the tag.
    /// </summary>
    public class CarStatsLoader : MonoBehaviour
    {
        [Header("Available Car Types")]
        [Tooltip("All car types available — SAME ORDER as CharacterSelectUI!")]
        [SerializeField] private CarStats[] availableCarTypes;

        private const string BASE_KEY = "SelectedCarTypeIndex";

        private void Awake()
        {
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "DesignerTuning")
                return;

            // In singleplayer the tag is already correct on the prefab so load immediately
            // In multiplayer SpawnManager calls LoadForCurrentTag() after setting the tag
            if (GameplayManager.instance != null &&
                GameplayManager.instance.GetCurrentGameMode() == GameMode.Singleplayer)
                LoadAndApplySelectedCarStats();
        }

        /// <summary>
        /// Called by SpawnManager after SetPlayerIdentity() so the tag is correct.
        /// </summary>
        public void LoadForCurrentTag()
        {
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "DesignerTuning")
                return;

            LoadAndApplySelectedCarStats();
        }

        private void LoadAndApplySelectedCarStats()
        {
            if (!ValidateCarTypesArray()) return;

            int      selectedIndex = LoadSelectedIndexFromPlayerPrefs();
            CarStats selectedStats = availableCarTypes[selectedIndex];

            ApplyStatsToComponents(selectedStats);
            LogStatsApplied(selectedStats, selectedIndex);
        }

        private bool ValidateCarTypesArray()
        {
            if (availableCarTypes == null || availableCarTypes.Length == 0)
            {
                Debug.LogError($"[{nameof(CarStatsLoader)}] No car types assigned on {gameObject.name}!");
                return false;
            }
            return true;
        }

        private int LoadSelectedIndexFromPlayerPrefs()
        {
            string key       = GetPlayerPrefsKey();
            int    savedIndex = PlayerPrefs.GetInt(key, 0);
            return Mathf.Clamp(savedIndex, 0, availableCarTypes.Length - 1);
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

        private void ApplyStatsToComponents(CarStats stats)
        {
            ApplyStatsToCarController(stats);
            ApplyStatsToCarHealth(stats);
        }

        private void ApplyStatsToCarController(CarStats stats)
        {
            CarController carController = GetComponent<CarController>();

            if (carController == null)
            {
                Debug.LogWarning($"[{nameof(CarStatsLoader)}] No CarController found on {gameObject.name}");
                return;
            }

            var field = typeof(CarController).GetField("carStats",
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);

            if (field != null)
                field.SetValue(carController, stats);
            else
                Debug.LogError($"[{nameof(CarStatsLoader)}] Could not find carStats field in CarController!");
        }

        private void ApplyStatsToCarHealth(CarStats stats)
        {
            CarHealth carHealth = GetComponent<CarHealth>();

            if (carHealth == null)
            {
                Debug.LogWarning($"[{nameof(CarStatsLoader)}] No CarHealth found on {gameObject.name}");
                return;
            }

            var field = typeof(CarHealth).GetField("carStats",
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);

            if (field != null)
                field.SetValue(carHealth, stats);
            else
                Debug.LogWarning($"[{nameof(CarStatsLoader)}] Could not find carStats field in CarHealth");
        }

        private void LogStatsApplied(CarStats stats, int index)
        {
            Debug.Log($"━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
            Debug.Log($"[{nameof(CarStatsLoader)}] {gameObject.name} loaded car type!");
            Debug.Log($"[{nameof(CarStatsLoader)}] Key: {GetPlayerPrefsKey()} | Index: {index} | Car: {stats.name}");
            Debug.Log($"━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
        }

        /// <summary>
        /// Apply new car stats at runtime (e.g. from TuningManager).
        /// </summary>
        public void ApplyCarStats(CarStats newStats)
        {
            if (newStats == null)
            {
                Debug.LogWarning($"[{nameof(CarStatsLoader)}] Cannot apply null CarStats!");
                return;
            }

            ApplyStatsToComponents(newStats);
            Debug.Log($"[{nameof(CarStatsLoader)}] ✓ Applied {newStats.name} to {gameObject.name} at runtime!");
        }
    }
}