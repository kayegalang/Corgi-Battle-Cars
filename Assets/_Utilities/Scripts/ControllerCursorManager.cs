using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

/// <summary>
/// Hides the mouse cursor and disables mouse hover on buttons when a controller is connected.
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

            // Disable mouse input module so cursor can't hover over buttons
            if (EventSystem.current != null)
            {
                var mouseModule = EventSystem.current.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
                if (mouseModule != null)
                    mouseModule.enabled = false;
            }
        }
        else
        {
            Cursor.visible   = true;
            Cursor.lockState = CursorLockMode.None;

            if (EventSystem.current != null)
            {
                var mouseModule = EventSystem.current.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
                if (mouseModule != null)
                    mouseModule.enabled = true;
            }
        }
    }
}