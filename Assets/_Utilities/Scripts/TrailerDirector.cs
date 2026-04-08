using _Bot.Scripts;
using _PowerUps.ScriptableObjects;
using _PowerUps.Scripts;
using UnityEngine;

namespace _Gameplay.Scripts
{
    /// <summary>
    /// Hotkey controller for trailer filming.
    /// Drop on any GameObject in the Trailer scene.
    ///
    /// CONTROLS:
    ///   1  — All bots chase nearest power-up
    ///   2  — One bot activates Squirrel power-up
    ///   3  — One bot activates Zoomies power-up
    ///   4  — One bot activates Poop power-up
    /// </summary>
    public class TrailerDirector : MonoBehaviour
    {
        [Header("Power-Up ScriptableObjects")]
        [Tooltip("Drag your Squirrel PowerUpObject ScriptableObject here")]
        [SerializeField] private PowerUpObject squirrelPowerUp;

        [Tooltip("Drag your Zoomies PowerUpObject ScriptableObject here")]
        [SerializeField] private PowerUpObject zoomiesPowerUp;

        [Tooltip("Drag your Poop PowerUpObject ScriptableObject here")]
        [SerializeField] private PowerUpObject poopPowerUp;

        // ═══════════════════════════════════════════════
        //  UPDATE
        // ═══════════════════════════════════════════════

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Alpha1)) ForceBotsChasePowerUp();
            if (Input.GetKeyDown(KeyCode.Alpha2)) ActivatePowerUpOnBot(squirrelPowerUp,  "Squirrel");
            if (Input.GetKeyDown(KeyCode.Alpha3)) ActivatePowerUpOnBot(zoomiesPowerUp,   "Zoomies");
            if (Input.GetKeyDown(KeyCode.Alpha4)) ActivatePowerUpOnBot(poopPowerUp,      "Poop");
        }

        // ═══════════════════════════════════════════════
        //  1 — ALL BOTS CHASE POWER-UP
        // ═══════════════════════════════════════════════

        private void ForceBotsChasePowerUp()
        {
            BotAI[] bots = FindObjectsByType<BotAI>(FindObjectsSortMode.None);

            if (bots.Length == 0)
            {
                Debug.LogWarning("[TrailerDirector] No bots found in scene!");
                return;
            }

            int triggered = 0;
            foreach (BotAI bot in bots)
            {
                if (bot.ForceChaseNearestPowerUp())
                    triggered++;
            }

            Debug.Log($"[TrailerDirector] Forced {triggered}/{bots.Length} bots to chase power-ups!");
        }

        // ═══════════════════════════════════════════════
        //  2 / 3 / 4 — ACTIVATE POWER-UP ON ONE BOT
        // ═══════════════════════════════════════════════

        private void ActivatePowerUpOnBot(PowerUpObject powerUp, string powerUpName)
        {
            if (powerUp == null)
            {
                Debug.LogWarning($"[TrailerDirector] {powerUpName} PowerUpObject not assigned in Inspector!");
                return;
            }

            // Find a bot that doesn't already have a power-up active
            PowerUpHandler[] handlers = FindObjectsByType<PowerUpHandler>(FindObjectsSortMode.None);

            foreach (PowerUpHandler handler in handlers)
            {
                // Only target bots
                BotAI bot = handler.GetComponent<BotAI>();
                if (bot == null) continue;

                // Skip if already has one
                if (handler.HasHeldPowerUp()) continue;

                handler.ActivatePowerUp(powerUp);
                Debug.Log($"[TrailerDirector] Activated {powerUpName} on {handler.gameObject.name}!");
                return;
            }

            // If all bots already have power-ups, just force it on the first bot
            foreach (PowerUpHandler handler in handlers)
            {
                BotAI bot = handler.GetComponent<BotAI>();
                if (bot == null) continue;

                handler.ActivatePowerUp(powerUp);
                Debug.Log($"[TrailerDirector] Force-activated {powerUpName} on {handler.gameObject.name}!");
                return;
            }

            Debug.LogWarning("[TrailerDirector] No bot PowerUpHandlers found!");
        }
    }
}
