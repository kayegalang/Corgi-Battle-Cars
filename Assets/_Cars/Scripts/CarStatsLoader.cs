using _Cars.ScriptableObjects;
using UnityEngine;

namespace _Cars.Scripts
{
    /// <summary>
    /// Loads the player's selected car type from CharacterSelect and applies stats.
    /// Add this to the Player prefab!
    /// </summary>
    public class CarStatsLoader : MonoBehaviour
    {
        [Header("Available Car Types")]
        [Tooltip("All car types available (Tank, Speedster, Aerialist, Bruiser) - SAME ORDER as CharacterSelectUI!")]
        [SerializeField] private CarStats[] availableCarTypes;

        private const string SELECTED_INDEX_KEY = "SelectedCarTypeIndex";

        private void Awake()
        {
            // Don't load from PlayerPrefs in the designer tuning scene
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "DesignerTuning")
                return;

            LoadAndApplySelectedCarStats();
        }

        private void LoadAndApplySelectedCarStats()
        {
            if (!ValidateCarTypesArray())
            {
                return;
            }

            int selectedIndex = LoadSelectedIndexFromPlayerPrefs();
            CarStats selectedStats = GetCarStatsAtIndex(selectedIndex);

            ApplyStatsToComponents(selectedStats);
            LogStatsApplied(selectedStats, selectedIndex);
        }

        private bool ValidateCarTypesArray()
        {
            if (availableCarTypes == null || availableCarTypes.Length == 0)
            {
                Debug.LogError($"[{nameof(CarStatsLoader)}] No car types assigned! Assign CarStats array in Inspector!");
                return false;
            }

            return true;
        }

        private int LoadSelectedIndexFromPlayerPrefs()
        {
            int savedIndex = PlayerPrefs.GetInt(SELECTED_INDEX_KEY, 0);
            int clampedIndex = Mathf.Clamp(savedIndex, 0, availableCarTypes.Length - 1);

            return clampedIndex;
        }

        private CarStats GetCarStatsAtIndex(int index)
        {
            return availableCarTypes[index];
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

            // Use reflection to set the private carStats field
            var carStatsField = typeof(CarController).GetField("carStats", 
                System.Reflection.BindingFlags.NonPublic | 
                System.Reflection.BindingFlags.Instance);

            if (carStatsField != null)
            {
                carStatsField.SetValue(carController, stats);
                Debug.Log($"[{nameof(CarStatsLoader)}] CarStats applied to CarController!");
            }
            else
            {
                Debug.LogError($"[{nameof(CarStatsLoader)}] Could not find carStats field in CarController!");
            }
        }

        private void ApplyStatsToCarHealth(CarStats stats)
        {
            CarHealth carHealth = GetComponent<CarHealth>();

            if (carHealth == null)
            {
                Debug.LogWarning($"[{nameof(CarStatsLoader)}] No CarHealth found on {gameObject.name}");
                return;
            }

            // Use reflection to set the private carStats field
            var carStatsField = typeof(CarHealth).GetField("carStats", 
                System.Reflection.BindingFlags.NonPublic | 
                System.Reflection.BindingFlags.Instance);

            if (carStatsField != null)
            {
                carStatsField.SetValue(carHealth, stats);
                Debug.Log($"[{nameof(CarStatsLoader)}] CarStats applied to CarHealth!");
            }
            else
            {
                Debug.LogWarning($"[{nameof(CarStatsLoader)}] Could not find carStats field in CarHealth (might not have it yet)");
            }
        }

        private void LogStatsApplied(CarStats stats, int index)
        {
            Debug.Log($"━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
            Debug.Log($"[{nameof(CarStatsLoader)}] {gameObject.name} loaded car type!");
            Debug.Log($"[{nameof(CarStatsLoader)}] Selected Index: {index}");
            Debug.Log($"[{nameof(CarStatsLoader)}] Car Type: {stats.name}");
            Debug.Log($"[{nameof(CarStatsLoader)}] Max Health: {stats.MaxHealth}");
            Debug.Log($"[{nameof(CarStatsLoader)}] Max Speed: {stats.MaxSpeed}");
            Debug.Log($"[{nameof(CarStatsLoader)}] Acceleration: {stats.Acceleration}");
            Debug.Log($"[{nameof(CarStatsLoader)}] Jump Force: {stats.JumpForce}");
            Debug.Log($"━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
        }
        
        /// <summary>
        /// Apply new car stats at runtime (for tuning scene).
        /// This allows the designer to change bot cars on the fly.
        /// </summary>
        public void ApplyCarStats(CarStats newStats)
        {
            if (newStats == null)
            {
                Debug.LogWarning($"[{nameof(CarStatsLoader)}] Cannot apply null CarStats!");
                return;
            }
            
            // Reuse existing methods to apply stats
            ApplyStatsToComponents(newStats);
            
            Debug.Log($"[{nameof(CarStatsLoader)}] ✓ Applied {newStats.name} to {gameObject.name} at runtime!");
        }
    }
}