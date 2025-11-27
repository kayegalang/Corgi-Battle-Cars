using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace _UI.Scripts
{
    /// <summary>
    /// Creates multiple health bar instances - one for each camera in split-screen
    /// Each health bar is on a specific layer so only that camera sees it
    /// Each health bar faces its respective camera
    /// </summary>
    public class HealthBarManager : MonoBehaviour
    {
        [Header("Health Bar Settings")]
        [SerializeField] private GameObject healthBarPrefab; // The health bar canvas prefab
        [SerializeField] private Vector3 healthBarOffset = new Vector3(0, 2, 0); // Offset above player
        
        private Camera[] playerCameras;
        private GameObject[] healthBarInstances;
        private int[] cameraLayers = { 6, 7, 8, 9 }; // Player layers (Player1, Player2, Player3, Player4)
        
        void Start()
        {
            // Wait a frame for cameras to be set up
            Invoke(nameof(SetupHealthBars), 0.5f);
        }
        
        private void SetupHealthBars()
        {
            // Find all player cameras (not just any camera)
            Camera[] allCameras = FindObjectsByType<Camera>(FindObjectsSortMode.None);
            
            // Filter to only player cameras (on player layers 6, 7, 8, 9)
            List<Camera> playerCamerasList = new List<Camera>();
            foreach (Camera cam in allCameras)
            {
                if (cam != null && cam.gameObject.layer >= 6 && cam.gameObject.layer <= 9)
                {
                    playerCamerasList.Add(cam);
                }
            }
            
            // Sort cameras by their tag (PlayerOne, PlayerTwo, etc.) to ensure consistent ordering
            playerCamerasList.Sort((a, b) => 
            {
                string tagA = a.transform.root.tag;
                string tagB = b.transform.root.tag;
                return string.Compare(tagA, tagB, System.StringComparison.Ordinal);
            });
            
            playerCameras = playerCamerasList.ToArray();
            
            if (playerCameras.Length == 0)
            {
                Debug.LogWarning($"{gameObject.name}: No player cameras found! Retrying in 0.5s...");
                Invoke(nameof(SetupHealthBars), 0.5f); // Retry
                return;
            }
            
            Debug.Log($"{gameObject.name}: Creating {playerCameras.Length} health bar instances");
            
            // Create one health bar for each camera found
            healthBarInstances = new GameObject[playerCameras.Length];
            
            for (int i = 0; i < playerCameras.Length; i++)
            {
                if (playerCameras[i] == null) continue;
                
                // Assign to player layers sequentially (6, 7, 8, 9)
                int healthBarLayer = cameraLayers[i];
                
                // Create a health bar instance for this camera
                GameObject healthBarInstance = Instantiate(healthBarPrefab, transform);
                healthBarInstance.name = $"HealthBar_Player{i + 1}_Layer{healthBarLayer}";
                healthBarInstances[i] = healthBarInstance;
                
                // Set the health bar to the assigned player layer
                SetLayerRecursively(healthBarInstance, healthBarLayer);
                
                // Add a component to make it face this specific camera
                CameraFacing cameraFacing = healthBarInstance.AddComponent<CameraFacing>();
                cameraFacing.SetTargetCamera(playerCameras[i]);
                
                string playerTag = playerCameras[i].transform.root.tag;
                Debug.Log($"✓ Created {healthBarInstance.name} on layer {healthBarLayer} (for {playerTag} camera: {playerCameras[i].name})");
            }
        }
        
        private Camera GetCameraForLayer(int layer)
        {
            if (playerCameras == null) return null;
            
            foreach (Camera cam in playerCameras)
            {
                if (cam != null && cam.gameObject.layer == layer)
                {
                    return cam;
                }
            }
            
            return null;
        }
        
        private void SetLayerRecursively(GameObject obj, int layer)
        {
            obj.layer = layer;
            foreach (Transform child in obj.transform)
            {
                SetLayerRecursively(child.gameObject, layer);
            }
        }
        
        void LateUpdate()
        {
            // Update position of all health bar instances
            if (healthBarInstances == null) return;
            
            Vector3 worldPosition = transform.position + healthBarOffset;
            
            // Debug every few frames
            if (Time.frameCount % 120 == 0)
            {
                Debug.Log($"{gameObject.name} health bars at: {worldPosition}, instances: {healthBarInstances.Length}");
            }
            
            foreach (GameObject healthBar in healthBarInstances)
            {
                if (healthBar != null)
                {
                    healthBar.transform.position = worldPosition;
                }
            }
        }
        
        /// <summary>
        /// Call this to update all health bar values
        /// </summary>
        public void UpdateAllHealthBars(float healthPercent)
        {
            if (healthBarInstances == null) return;
            
            foreach (GameObject healthBar in healthBarInstances)
            {
                if (healthBar != null)
                {
                    Slider slider = healthBar.GetComponentInChildren<Slider>();
                    if (slider != null)
                    {
                        slider.value = healthPercent;
                    }
                }
            }
        }
    }
    
    /// <summary>
    /// Helper component that makes a health bar always face a specific camera
    /// </summary>
    public class CameraFacing : MonoBehaviour
    {
        private Camera targetCamera;
        private int targetLayer = -1;
        private float cameraSearchTimer = 0f;
        private const float CAMERA_SEARCH_INTERVAL = 0.5f;
        
        public void SetTargetCamera(Camera camera)
        {
            targetCamera = camera;
        }
        
        public void SetTargetLayer(int layer)
        {
            targetLayer = layer;
        }
        
        void LateUpdate()
        {
            // If we don't have a camera yet, try to find one
            if (targetCamera == null && targetLayer >= 0)
            {
                cameraSearchTimer += Time.deltaTime;
                if (cameraSearchTimer >= CAMERA_SEARCH_INTERVAL)
                {
                    cameraSearchTimer = 0f;
                    TryFindCamera();
                }
            }
            
            // Face the target camera
            if (targetCamera != null && targetCamera)
            {
                Vector3 directionToCamera = targetCamera.transform.position - transform.position;
                directionToCamera.y = 0; // Keep upright
                
                if (directionToCamera.sqrMagnitude > 0.001f)
                {
                    transform.rotation = Quaternion.LookRotation(directionToCamera);
                }
            }
        }
        
        private void TryFindCamera()
        {
            Camera[] allCameras = FindObjectsByType<Camera>(FindObjectsSortMode.None);
            
            foreach (Camera cam in allCameras)
            {
                if (cam != null && cam.gameObject.layer == targetLayer)
                {
                    targetCamera = cam;
                    Debug.Log($"{gameObject.name}: Found target camera on layer {targetLayer}");
                    break;
                }
            }
        }
    }
}