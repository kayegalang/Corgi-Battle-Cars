using UnityEngine;
using UnityEngine.UI;
using _Utilities.Scripts;

namespace _UI.Scripts
{
    public class HealthBarManager : MonoBehaviour
    {
        [Header("Health Bar Settings")]
        [SerializeField] private GameObject healthBarPrefab;
        [SerializeField] private Vector3 healthBarOffset = new Vector3(0, 2, 0);
        
        // [Header("Setup Timing")]
        // [SerializeField] private float setupDelay = 0.5f;
        // [SerializeField] private float setupRetryDelay = 0.5f;
        
        private Camera[] playerCameras;
        private GameObject[] healthBarInstances;
        
        void Start()
        {
            // Invoke(nameof(SetupHealthBars), setupDelay);
            SetupHealthBars();
        }
        
        private void SetupHealthBars()
        {
            playerCameras = CameraUtility.FindPlayerCameras();
            
            // if (playerCameras.Length == 0)
            // {
            //     Debug.LogWarning($"{gameObject.name}: No player cameras found! Retrying in {setupRetryDelay}s...");
            //     Invoke(nameof(SetupHealthBars), setupRetryDelay);
            //     return;
            // }
            
            CreateHealthBarInstances();
        }
        
        private void CreateHealthBarInstances()
        {
            healthBarInstances = new GameObject[playerCameras.Length];
    
            for (int i = 0; i < playerCameras.Length; i++)
            {
                if (playerCameras[i] == null) continue;
        
                // Use the camera's ACTUAL layer, not an index-based layer!
                int healthBarLayer = playerCameras[i].gameObject.layer;
        
                GameObject healthBarInstance = Instantiate(healthBarPrefab, transform);
                healthBarInstance.name = $"HealthBar_Player{i + 1}_Layer{healthBarLayer}";
                healthBarInstances[i] = healthBarInstance;
        
                SetLayerRecursively(healthBarInstance, healthBarLayer);
        
                CameraFacing cameraFacing = healthBarInstance.AddComponent<CameraFacing>();
                cameraFacing.SetTargetCamera(playerCameras[i]);
            }
        }

        private GameObject CreateHealthBarInstance(int i, int healthBarLayer)
        {
            GameObject healthBarInstance = Instantiate(healthBarPrefab, transform);
            healthBarInstance.name = $"HealthBar_Player{i + 1}_Layer{healthBarLayer}";
            healthBarInstances[i] = healthBarInstance;
            return healthBarInstance;
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
            if (healthBarInstances == null) return;
            
            Vector3 worldPosition = transform.position + healthBarOffset;
            
            foreach (GameObject healthBar in healthBarInstances)
            {
                if (healthBar != null)
                {
                    healthBar.transform.position = worldPosition;
                }
            }
        }
        
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
}