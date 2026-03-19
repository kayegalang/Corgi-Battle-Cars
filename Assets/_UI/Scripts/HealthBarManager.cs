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

        private Camera[]      playerCameras;
        private GameObject[]  healthBarInstances;

        // ═══════════════════════════════════════════════
        //  UNITY LIFECYCLE
        // ═══════════════════════════════════════════════

        private void Start()
        {
            SetupHealthBars();
        }

        private void LateUpdate()
        {
            if (healthBarInstances == null) return;

            Vector3 worldPosition = transform.position + healthBarOffset;

            foreach (GameObject healthBar in healthBarInstances)
            {
                if (healthBar != null)
                    healthBar.transform.position = worldPosition;
            }
        }

        // ═══════════════════════════════════════════════
        //  SETUP
        // ═══════════════════════════════════════════════

        private void SetupHealthBars()
        {
            playerCameras = CameraUtility.FindPlayerCameras();
            CreateHealthBarInstances();
        }

        private void CreateHealthBarInstances()
        {
            healthBarInstances = new GameObject[playerCameras.Length];

            for (int i = 0; i < playerCameras.Length; i++)
            {
                if (playerCameras[i] == null) continue;

                int healthBarLayer = playerCameras[i].gameObject.layer;

                GameObject instance = Instantiate(healthBarPrefab, transform);
                instance.name       = $"HealthBar_Player{i + 1}_Layer{healthBarLayer}";
                healthBarInstances[i] = instance;

                SetLayerRecursively(instance, healthBarLayer);

                CameraFacing cameraFacing = instance.AddComponent<CameraFacing>();
                cameraFacing.SetTargetCamera(playerCameras[i]);
            }
        }

        private void SetLayerRecursively(GameObject obj, int layer)
        {
            obj.layer = layer;
            foreach (Transform child in obj.transform)
                SetLayerRecursively(child.gameObject, layer);
        }

        // ═══════════════════════════════════════════════
        //  PUBLIC — called by CarHealth when any car spawns
        // ═══════════════════════════════════════════════

        /// <summary>
        /// Destroys and rebuilds all health bar instances using the current
        /// set of active player cameras. Call this whenever a car spawns or
        /// respawns so every car picks up the new camera.
        /// </summary>
        public void RefreshHealthBars()
        {
            // Destroy existing instances
            if (healthBarInstances != null)
            {
                foreach (var bar in healthBarInstances)
                    if (bar != null) Destroy(bar);
            }

            // Rebuild with whatever cameras exist right now
            SetupHealthBars();
        }

        // ═══════════════════════════════════════════════
        //  UPDATE VALUE
        // ═══════════════════════════════════════════════

        public void UpdateAllHealthBars(float healthPercent)
        {
            if (healthBarInstances == null) return;

            foreach (GameObject healthBar in healthBarInstances)
            {
                if (healthBar == null) continue;

                Slider slider = healthBar.GetComponentInChildren<Slider>();
                if (slider != null)
                    slider.value = healthPercent;
            }
        }
    }
}