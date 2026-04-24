using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Hides the mouse cursor when a controller is connected.
/// Add to a persistent GameObject (DontDestroyOnLoad) in your first scene.
/// </summary>
public class ControllerCursorManager : MonoBehaviour
{
    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        UpdateCursor();
    }

    private void Update()
    {
        UpdateCursor();
    }

    private void UpdateCursor()
    {
        bool controllerConnected = Gamepad.all.Count > 0;

        if (controllerConnected)
        {
            Cursor.visible   = false;
            Cursor.lockState = CursorLockMode.Confined;
        }
    }
}
