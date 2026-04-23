using _Cars.ScriptableObjects;
using _Cars.Scripts;
using _Projectiles.ScriptableObjects;
using UnityEngine;

/// <summary>
/// Add to any GameObject in the Prototype Map scene.
/// Sets PlayerPrefs AND applies ScriptableObject stats to all
/// car components so you can test without going through character select.
/// Remove or disable when done testing!
/// </summary>
public class VisualTestBootstrapper : MonoBehaviour
{
    [Header("Test Selection")]
    [Tooltip("Index of the car to test (0 = KorgiKart)")]
    [SerializeField] private int carIndex    = 0;

    [Tooltip("Index of the weapon to test (0 = bArK-47)")]
    [SerializeField] private int weaponIndex = 0;

    [Header("Assets — must match same order as CharacterSelectUI")]
    [SerializeField] private CarStats[]         carTypes;
    [SerializeField] private ProjectileObject[] weaponTypes;

    private void Awake()
    {
        // Set PlayerPrefs so CarVisualLoader loads the right models
        PlayerPrefs.SetInt("SelectedCarTypeIndex",    carIndex);
        PlayerPrefs.SetInt("SelectedWeaponTypeIndex", weaponIndex);
        PlayerPrefs.Save();

        Debug.Log($"[VisualTestBootstrapper] Set car={carIndex}, weapon={weaponIndex}");
    }

    private void Start()
    {
        // Start() runs after CarVisualLoader.Awake() so components are ready
        ApplyStats();
        EnableGameplay();
    }

    // ═══════════════════════════════════════════════
    //  APPLY STATS
    // ═══════════════════════════════════════════════

    private void ApplyStats()
    {
        if (carTypes == null || weaponTypes == null) return;

        int    clampedCar    = Mathf.Clamp(carIndex,    0, carTypes.Length    - 1);
        int    clampedWeapon = Mathf.Clamp(weaponIndex, 0, weaponTypes.Length - 1);

        CarStats         selectedCar    = carTypes[clampedCar];
        ProjectileObject selectedWeapon = weaponTypes[clampedWeapon];

        // Apply to all players/bots in the scene
        foreach (CarShooter shooter in FindObjectsByType<CarShooter>(FindObjectsSortMode.None))
        {
            shooter.SetProjectileType(selectedWeapon);
            Debug.Log($"[VisualTestBootstrapper] Applied weapon '{selectedWeapon.ProjectileName}' to {shooter.gameObject.name}");
        }

        foreach (CarStatsLoader loader in FindObjectsByType<CarStatsLoader>(FindObjectsSortMode.None))
        {
            loader.ApplyCarStats(selectedCar);
            Debug.Log($"[VisualTestBootstrapper] Applied car stats '{selectedCar.CarName}' to {loader.gameObject.name}");
        }
    }

    // ═══════════════════════════════════════════════
    //  ENABLE GAMEPLAY
    // ═══════════════════════════════════════════════

    private void EnableGameplay()
    {
        foreach (var shooter in FindObjectsByType<CarShooter>(FindObjectsSortMode.None))
            shooter.EnableGameplay();

        foreach (var uiManager in FindObjectsByType<_UI.Scripts.PlayerUIManager>(FindObjectsSortMode.None))
            uiManager.EnableGameplay();
    }
}