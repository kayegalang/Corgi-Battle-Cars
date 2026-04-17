using UnityEngine;
using UnityEngine.InputSystem;

namespace _Utilities.Scripts
{
    /// <summary>
    /// TEMPORARY — add to any GameObject.
    /// Press R to test raw gamepad rumble completely independent of game code.
    /// Remove once vibration is confirmed working.
    /// </summary>
    public class RumbleTest : MonoBehaviour
    {
        private void Update()
        {
            // Press R — full rumble on every connected gamepad
            if (Input.GetKeyDown(KeyCode.R))
            {
                Debug.Log($"[RumbleTest] Gamepads connected: {Gamepad.all.Count}");

                foreach (var gp in Gamepad.all)
                {
                    Debug.Log($"[RumbleTest] Rumbling: {gp.displayName}");
                    gp.SetMotorSpeeds(1f, 1f);
                }

                // Also try Gamepad.current
                if (Gamepad.current != null)
                {
                    Debug.Log($"[RumbleTest] Gamepad.current: {Gamepad.current.displayName}");
                    Gamepad.current.SetMotorSpeeds(1f, 1f);
                }
                else
                {
                    Debug.LogWarning("[RumbleTest] Gamepad.current is NULL!");
                }
            }

            // Press T — stop all rumble
            if (Input.GetKeyDown(KeyCode.T))
            {
                foreach (var gp in Gamepad.all)
                    gp.SetMotorSpeeds(0f, 0f);

                Gamepad.current?.SetMotorSpeeds(0f, 0f);
                Debug.Log("[RumbleTest] Rumble stopped.");
            }
        }
    }
}