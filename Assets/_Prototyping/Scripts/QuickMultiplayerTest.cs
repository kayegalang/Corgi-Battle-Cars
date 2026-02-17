using _Gameplay.Scripts;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Quick test for forcing multiplayer mode without controllers.
/// Attach to any GameObject in the MAIN MENU scene.
/// Press 2/3/4 keys to instantly start that many players.
/// All players will use keyboard (they'll move together, but lets you test layouts).
/// </summary>
public class QuickMultiplayerTest : MonoBehaviour
{
    void Update()
    {
        if (Keyboard.current == null) return;

        // Press 2, 3, or 4 to instantly start multiplayer with that many players
        if (Keyboard.current.digit2Key.wasPressedThisFrame)
        {
            StartMultiplayer(2);
        }
        else if (Keyboard.current.digit3Key.wasPressedThisFrame)
        {
            StartMultiplayer(3);
        }
        else if (Keyboard.current.digit4Key.wasPressedThisFrame)
        {
            StartMultiplayer(4);
        }
    }

    private void StartMultiplayer(int playerCount)
    {
        if (GameplayManager.instance == null)
        {
            Debug.LogError("[QuickMultiplayerTest] GameplayManager.instance is null!");
            return;
        }

        Debug.Log($"[QuickMultiplayerTest] Starting {playerCount}-player game");

        // Force multiplayer mode and player count
        GameplayManager.instance.SetGameMode(GameMode.Multiplayer);
        GameplayManager.instance.SetMultiplayerPlayerCount(playerCount);

        // Select a map (use the first one that exists)
        GameplayManager.instance.SetMap("Park Map");

        // Start the game
        GameplayManager.instance.StartGame();
    }
}