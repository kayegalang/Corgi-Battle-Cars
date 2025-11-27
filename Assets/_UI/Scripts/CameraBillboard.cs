using UnityEngine;

namespace _UI.Scripts
{
    /// <summary>
    /// Makes a UI element always face the camera using constraint-based rotation
    /// Works with split-screen by facing the closest camera
    /// Add this to your health bar canvas GameObject
    /// </summary>
    public class CameraBillboard : MonoBehaviour
    {
        [Header("Billboard Settings")]
        [SerializeField] private bool lockYAxis = true; // Keep health bar upright
        
        private Camera mainCamera;
        
        void Start()
        {
            // Use Camera.main as a fallback, but this will update dynamically
            mainCamera = Camera.main;
        }
        
        void LateUpdate()
        {
            // In split-screen, each camera renders separately
            // Camera.current gives us the camera that's currently rendering
            Camera currentCamera = Camera.current;
            
            if (currentCamera == null)
            {
                // Fallback if Camera.current is null (shouldn't happen during rendering)
                currentCamera = mainCamera;
            }
            
            if (currentCamera == null)
                return;
            
            // Make the health bar face the current rendering camera
            Vector3 directionToCamera = currentCamera.transform.position - transform.position;
            
            if (lockYAxis)
            {
                // Keep the health bar upright (don't tilt with camera)
                directionToCamera.y = 0;
            }
            
            if (directionToCamera.sqrMagnitude > 0.001f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(directionToCamera);
                transform.rotation = targetRotation;
            }
        }
    }
}