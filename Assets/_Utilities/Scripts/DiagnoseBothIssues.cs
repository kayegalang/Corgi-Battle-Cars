using UnityEngine;
using _Gameplay.Scripts;
using _Player.Scripts;
using _UI.Scripts;
public class DiagnoseBothIssues : MonoBehaviour
{
    void Start()
    {
        Invoke(nameof(RunDiagnostics), 0.5f);
    }
    
    void RunDiagnostics()
    {
        Debug.Log("========================================");
        Debug.Log("DIAGNOSTIC REPORT");
        Debug.Log("========================================");
        
        // Issue 1: Check tracker
        Debug.Log("\n--- ISSUE 1: CONTROLLER TEXT ---");
        if (PlayerOneInputTracker.instance == null)
        {
            Debug.LogError("❌ PlayerOneInputTracker.instance is NULL!");
            Debug.LogError("   Create a PlayerOneInputTracker GameObject in scene");
        }
        else
        {
            bool usingController = PlayerOneInputTracker.instance.IsPlayerOneUsingController();
            Debug.Log($"✓ Tracker exists");
            Debug.Log($"✓ PlayerOne should use: {(usingController ? "CONTROLLER" : "KEYBOARD")}");
        }
        
        // Check UIFirstSelected
        UIFirstSelected[] uiFirsts = FindObjectsByType<UIFirstSelected>(FindObjectsSortMode.None);
        if (uiFirsts.Length == 0)
        {
            Debug.LogError("❌ No UIFirstSelected components found in scene!");
            Debug.LogError("   Add UIFirstSelected.cs to your MainMenu panel");
        }
        else
        {
            Debug.Log($"✓ Found {uiFirsts.Length} UIFirstSelected component(s)");
        }
        
        // Issue 2: Check GameplayManager method
        Debug.Log("\n--- ISSUE 2: JOIN SCREEN SHOWING ---");
        if (GameplayManager.instance == null)
        {
            Debug.LogError("❌ GameplayManager.instance is NULL!");
        }
        else
        {
            try
            {
                GameMode mode = GameplayManager.instance.GetCurrentGameMode();
                Debug.Log($"✓ GameplayManager.GetCurrentGameMode() works!");
                Debug.Log($"✓ Current mode: {mode}");
            }
            catch
            {
                Debug.LogError("❌ GameplayManager.GetCurrentGameMode() does NOT exist!");
                Debug.LogError("   Add this method to GameplayManager.cs:");
                Debug.LogError("   public GameMode GetCurrentGameMode() { return currentGameMode; }");
            }
        }
        
        Debug.Log("\n========================================");
    }
}