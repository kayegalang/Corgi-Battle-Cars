using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Hides the mouse cursor whenever any gamepad is connected.
/// Shows it again if all gamepads are disconnected.
/// Add to any persistent GameObject (DontDestroyOnLoad).
/// </summary>
public class ControllerCursorHider : MonoBehaviour
{
    private void OnEnable()
    {
        InputSystem.onDeviceChange += OnDeviceChange;
    }

    private void OnDisable()
    {
        InputSystem.onDeviceChange -= OnDeviceChange;
    }

    private void Start()
    {
        // Apply immediately on startup based on current connected devices
        UpdateCursorVisibility();
    }

    private void OnDeviceChange(InputDevice device, InputDeviceChange change)
    {
        if (device is Gamepad)
            UpdateCursorVisibility();
    }

    private void UpdateCursorVisibility()
    {
        bool controllerConnected = Gamepad.all.Count > 0;

        Cursor.visible   = !controllerConnected;
        Cursor.lockState = controllerConnected
            ? CursorLockMode.Locked
            : CursorLockMode.None;

        Debug.Log($"[ControllerCursorHider] Controller connected: {controllerConnected} — cursor {(controllerConnected ? "hidden" : "shown")}");
    }
}
