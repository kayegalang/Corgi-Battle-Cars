using UnityEngine;
using UnityEngine.InputSystem;

namespace _PowerUps.Scripts
{
    /// <summary>
    /// Temporary debug script — add to the same GameObject as PowerUpPickup.
    /// Logs exactly what's happening with proximity vibration every second.
    /// Remove once vibration is working.
    /// </summary>
    public class PowerUpProximityDebugger : MonoBehaviour
    {
        private float timer = 0f;
        private const float LOG_INTERVAL = 1f;

        private static readonly string[] PlayerTags =
            { "PlayerOne", "PlayerTwo", "PlayerThree", "PlayerFour" };

        private void Update()
        {
            timer += Time.deltaTime;
            if (timer < LOG_INTERVAL) return;
            timer = 0f;

            Debug.Log("═══ PowerUp Proximity Debug ═══");

            // 1 — Are we finding any players at all?
            int totalFound = 0;
            foreach (string tag in PlayerTags)
            {
                try
                {
                    var players = GameObject.FindGameObjectsWithTag(tag);
                    foreach (var p in players)
                    {
                        totalFound++;
                        float dist = Vector3.Distance(transform.position, p.transform.position);

                        // 2 — Does the player have a PlayerInput?
                        var pi = p.GetComponent<PlayerInput>();
                        if (pi == null)
                        {
                            Debug.LogWarning($"  [{tag}] FOUND but has NO PlayerInput component!");
                            continue;
                        }

                        // 3 — Does PlayerInput have a gamepad device?
                        bool hasGamepad = false;
                        foreach (var device in pi.devices)
                        {
                            if (device is Gamepad gp)
                            {
                                hasGamepad = true;
                                Debug.Log($"  [{tag}] dist={dist:F1}  scheme={pi.currentControlScheme}  gamepad={gp.displayName}");
                            }
                        }

                        if (!hasGamepad)
                            Debug.LogWarning($"  [{tag}] dist={dist:F1}  scheme={pi.currentControlScheme}  NO GAMEPAD DEVICE — keyboard player or device not assigned!");
                    }
                }
                catch { }
            }

            if (totalFound == 0)
                Debug.LogError("  No players found with any PlayerOne/Two/Three/Four tag!");

            Debug.Log("════════════════════════════════");
        }
    }
}