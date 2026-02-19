using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace _Player.Scripts
{
    public class PlayerDeviceTracker : MonoBehaviour
    {
        public static PlayerDeviceTracker instance;
        
        // Maps player tag to their device
        private Dictionary<string, InputDevice> playerDevices = new Dictionary<string, InputDevice>();
        
        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public void RecordPlayerDevice(string playerTag, InputDevice device)
        {
            if (device == null)
            {
                Debug.LogWarning($"[PlayerDeviceTracker] Cannot record null device for {playerTag}");
                return;
            }
            
            playerDevices[playerTag] = device;
            Debug.Log($"[PlayerDeviceTracker] Recorded {playerTag} → {device.displayName}");
        }
        
        public InputDevice GetPlayerDevice(string playerTag)
        {
            if (playerDevices.TryGetValue(playerTag, out InputDevice device))
            {
                return device;
            }
            
            Debug.LogWarning($"[PlayerDeviceTracker] No device recorded for {playerTag}");
            return null;
        }
        
        public void Clear()
        {
            playerDevices.Clear();
            Debug.Log($"[PlayerDeviceTracker] Cleared all device mappings");
        }
        
        public List<string> GetTrackedPlayers()
        {
            return new List<string>(playerDevices.Keys);
        }
    }
}