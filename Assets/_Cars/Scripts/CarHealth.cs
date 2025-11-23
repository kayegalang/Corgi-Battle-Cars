using _Bot.Scripts;
using _Gameplay.Scripts;
using _UI.Scripts;
using UnityEngine;

namespace _Cars.Scripts
{
    public class CarHealth : MonoBehaviour
    {
        [SerializeField] private SpawnManager spawnManager;
        [SerializeField] HealthBar healthBar;
        private readonly int maxHealth = 100;
        private int currentHealth;

        private bool isBot;
        private bool isDead = false;

        void Start()
        
        {
            isBot = GetComponent<BotAI>() != null;
            currentHealth = maxHealth;
            spawnManager = FindFirstObjectByType<SpawnManager>();
            isBot = GetComponent<BotAI>() != null;
            
            // Find the Canvas in this player's children (should be PlayerHealthCanvas)
            Canvas healthCanvas = GetComponentInChildren<Canvas>();
            
            Debug.Log($"Player {gameObject.name}: Found Canvas: {healthCanvas != null}");
            
            if (healthCanvas != null)
            {
                Debug.Log($"Player {gameObject.name}: Canvas name: {healthCanvas.gameObject.name}, render mode: {healthCanvas.renderMode}");
                
                if (healthCanvas.renderMode == RenderMode.WorldSpace)
                {
                    // Find the camera on this player's GameObject hierarchy
                    Camera playerCamera = GetComponentInChildren<Camera>();
                    
                    if (playerCamera != null)
                    {
                        int cameraLayer = playerCamera.gameObject.layer;
                        
                        // IMPORTANT: Set canvas and ALL children to the camera's layer
                        // This makes each camera only see its own health bar
                        healthCanvas.gameObject.layer = cameraLayer;
                        
                        // Set all children recursively
                        SetLayerRecursively(healthCanvas.gameObject, cameraLayer);
                        
                        // Set the world camera for event handling
                        healthCanvas.worldCamera = playerCamera;
                        
                        Debug.Log($"Player {gameObject.name}: Set health canvas and children to layer {cameraLayer}, camera: {playerCamera.name}");
                        Debug.Log($"Player {gameObject.name}: Camera instance ID: {playerCamera.GetInstanceID()}");
                    }
                    else
                    {
                        Debug.LogWarning($"Player {gameObject.name}: No camera found on player for health bar!");
                    }
                }
                else
                {
                    Debug.LogWarning($"Player {gameObject.name}: Canvas is not in WorldSpace mode!");
                }
            }
            else
            {
                Debug.LogWarning($"Player {gameObject.name}: No Canvas found in children!");
            }
        }
        
        private void SetLayerRecursively(GameObject obj, int layer)
        {
            obj.layer = layer;
            foreach (Transform child in obj.transform)
            {
                SetLayerRecursively(child.gameObject, layer);
            }
        }

        public void TakeDamage(int amount, GameObject shooter)
        {
            currentHealth -= amount;
            healthBar.UpdateHealthBar();
        
            if (currentHealth <= 0)
            {
                Die(shooter);
            }

            if (isBot)
            {
                GetComponent<BotAI>().OnHit(shooter.transform);
            }
        }

        private void Die(GameObject shooter)
        {
            if (isDead) return;
            isDead = true;
            
            PointsManager.instance.AddPoint(shooter.tag);
            
            spawnManager.Respawn(gameObject.tag);
            Destroy(gameObject);
        }

        public float GetHealthPercent()
        {
            return (float)currentHealth / maxHealth;
        }

        public bool GetIsBot()
        {
            return isBot;
        }
    }

 
}