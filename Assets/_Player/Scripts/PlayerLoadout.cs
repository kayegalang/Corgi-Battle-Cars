using _Cars.ScriptableObjects;
using _Projectiles.ScriptableObjects;
using UnityEngine;

/// <summary>
/// Stores each player's car and weapon selection in memory for the session.
/// Replaces PlayerPrefs — no file I/O, no sync issues between arrays.
/// 
/// Set by CharacterSelectUI (singleplayer) or PlayerCharacterSelectPanel (multiplayer).
/// Read by CarVisualLoader, CarStatsLoader, WeaponStatsLoader at spawn time.
/// </summary>
public static class PlayerLoadout
{
    private static CarStats[]         selectedCars    = new CarStats[4];
    private static ProjectileObject[] selectedWeapons = new ProjectileObject[4];

    // ═══════════════════════════════════════════════
    //  SET
    // ═══════════════════════════════════════════════

    public static void SetCar(int playerIndex, CarStats car)
    {
        if (!Valid(playerIndex)) return;
        selectedCars[playerIndex] = car;
        Debug.Log($"[PlayerLoadout] P{playerIndex + 1} car → {car?.CarName}");
    }

    public static void SetWeapon(int playerIndex, ProjectileObject weapon)
    {
        if (!Valid(playerIndex)) return;
        selectedWeapons[playerIndex] = weapon;
        Debug.Log($"[PlayerLoadout] P{playerIndex + 1} weapon → {weapon?.ProjectileName}");
    }

    // ═══════════════════════════════════════════════
    //  GET
    // ═══════════════════════════════════════════════

    public static CarStats GetCar(int playerIndex) =>
        Valid(playerIndex) ? selectedCars[playerIndex] : null;

    public static ProjectileObject GetWeapon(int playerIndex) =>
        Valid(playerIndex) ? selectedWeapons[playerIndex] : null;

    // ═══════════════════════════════════════════════
    //  CLEAR
    // ═══════════════════════════════════════════════

    public static void Clear()
    {
        selectedCars    = new CarStats[4];
        selectedWeapons = new ProjectileObject[4];
    }

    // ═══════════════════════════════════════════════
    //  HELPERS
    // ═══════════════════════════════════════════════

    private static bool Valid(int index) => index >= 0 && index < 4;
}
