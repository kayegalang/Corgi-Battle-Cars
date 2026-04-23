using _Cars.ScriptableObjects;
using _Gameplay.Scripts;
using UnityEngine;

namespace _Cars.Scripts
{
    public class CarStatsLoader : MonoBehaviour
    {
        [Header("Available Car Types")]
        [Tooltip("All car types available — SAME ORDER as CharacterSelectUI!")]
        [SerializeField] private CarStats[] availableCarTypes;

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
                LoadAndApplySelectedCarStats();
        }

        public void LoadForCurrentTag()
        {
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "DesignerTuning")
                return;

            LoadAndApplySelectedCarStats();
        }

        private void LoadAndApplySelectedCarStats()
        {
            if (!ValidateCarTypesArray()) return;

            CarStats selected = GetSelectedCarStats();
            if (selected == null) return;

            ApplyStatsToComponents(selected);
            Debug.Log($"[CarStatsLoader] {gameObject.name} → {selected.CarName}");
        }

        private CarStats GetSelectedCarStats()
        {
            // Try PlayerLoadout first — direct ScriptableObject reference, no index needed
            int playerIdx = GetPlayerIndex();
            CarStats fromLoadout = PlayerLoadout.GetCar(playerIdx);
            if (fromLoadout != null) return fromLoadout;

            // Fall back to PlayerPrefs index for backwards compatibility
            int saved = PlayerPrefs.GetInt("SelectedCarTypeIndex", 0);
            int index = Mathf.Clamp(saved, 0, availableCarTypes.Length - 1);
            return availableCarTypes[index];
        }

        private int GetPlayerIndex()
        {
            if (TagToIndex.TryGetValue(gameObject.tag, out int index))
                return index;
            return 0;
        }

        private bool ValidateCarTypesArray()
        {
            if (availableCarTypes == null || availableCarTypes.Length == 0)
            {
                Debug.LogError($"[CarStatsLoader] No car types assigned on {gameObject.name}!");
                return false;
            }
            return true;
        }

        private void ApplyStatsToComponents(CarStats stats)
        {
            ApplyStatsToCarController(stats);
            ApplyStatsToCarHealth(stats);
        }

        private void ApplyStatsToCarController(CarStats stats)
        {
            CarController carController = GetComponent<CarController>();
            if (carController == null) return;

            typeof(CarController)
                .GetField("carStats",
                    System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.Instance)
                ?.SetValue(carController, stats);
        }

        private void ApplyStatsToCarHealth(CarStats stats)
        {
            CarHealth carHealth = GetComponent<CarHealth>();
            if (carHealth == null) return;

            typeof(CarHealth)
                .GetField("carStats",
                    System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.Instance)
                ?.SetValue(carHealth, stats);
        }

        public void ApplyCarStats(CarStats newStats)
        {
            if (newStats == null) return;
            ApplyStatsToComponents(newStats);
            Debug.Log($"[CarStatsLoader] Applied {newStats.CarName} to {gameObject.name}");
        }
    }
}