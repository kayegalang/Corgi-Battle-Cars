using UnityEngine;
using UnityEngine.InputSystem;

namespace _UI.Scripts
{
    /// <summary>
    /// Add to any Back Button GameObject.
    /// Hides it when a controller is connected, shows it when only keyboard/mouse is connected.
    /// </summary>
    public class HideOnController : MonoBehaviour
    {
        private void OnEnable()
        {
            InputSystem.onDeviceChange += OnDeviceChange;
            UpdateVisibility();
        }

        private void OnDisable()
        {
            InputSystem.onDeviceChange -= OnDeviceChange;
        }

        private void OnDeviceChange(InputDevice device, InputDeviceChange change)
        {
            if (device is Gamepad)
                UpdateVisibility();
        }

        private void UpdateVisibility()
        {
            gameObject.SetActive(Gamepad.all.Count == 0);
        }
    }
}
