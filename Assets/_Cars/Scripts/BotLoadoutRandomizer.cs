using _Bot.Scripts;
using _Cars.Scripts;
using _Cars.ScriptableObjects;
using _Projectiles.ScriptableObjects;
using UnityEngine;

namespace _Cars.Scripts
{
    /// <summary>
    /// Randomly assigns a car type and weapon to a bot on spawn.
    /// Add to the bot prefab alongside BotAI.
    /// </summary>
    public class BotLoadoutRandomizer : MonoBehaviour
    {
        [Header("Available Loadouts")]
        [Tooltip("Same CarStats SOs as CharacterSelectUI — any will be picked randomly")]
        [SerializeField] private CarStats[] availableCarTypes;

        [Tooltip("Same ProjectileObject SOs as CharacterSelectUI — any will be picked randomly")]
        [SerializeField] private ProjectileObject[] availableWeaponTypes;

        private void Awake()
        {
            ApplyRandomCar();
            ApplyRandomWeapon();
        }

        private void ApplyRandomCar()
        {
            if (availableCarTypes == null || availableCarTypes.Length == 0) return;

            CarStats randomCar = availableCarTypes[Random.Range(0, availableCarTypes.Length)];

            // Apply to CarStatsLoader (handles CarController + CarHealth)
            CarStatsLoader statsLoader = GetComponent<CarStatsLoader>();
            if (statsLoader != null)
                statsLoader.ApplyCarStats(randomCar);

            // Apply directly to BotController (has its own carStats field)
            SetPrivateField(GetComponent<BotController>(), "carStats", randomCar);

            // Apply directly to CarHealth (has its own carStats field)
            SetPrivateField(GetComponent<CarHealth>(), "carStats", randomCar);

            Debug.Log($"[BotLoadoutRandomizer] {gameObject.name} → car: {randomCar.CarName}");
        }

        private void SetPrivateField(object target, string fieldName, object value)
        {
            if (target == null) return;
            target.GetType()
                .GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(target, value);
        }

        private void ApplyRandomWeapon()
        {
            if (availableWeaponTypes == null || availableWeaponTypes.Length == 0) return;

            ProjectileObject randomWeapon = availableWeaponTypes[Random.Range(0, availableWeaponTypes.Length)];

            BotAI botAI = GetComponent<BotAI>();
            if (botAI != null)
            {
                botAI.SetProjectile(randomWeapon);
                Debug.Log($"[BotLoadoutRandomizer] {gameObject.name} → weapon: {randomWeapon.ProjectileName}");
            }
        }
    }
}