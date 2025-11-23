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
            
            // Find the Canvas and set it to the correct layer for split-screen
            Canvas healthCanvas = GetComponentInChildren<Canvas>();
            
            if (healthCanvas != null && healthCanvas.renderMode == RenderMode.WorldSpace)
            {
                Camera playerCamera = GetComponentInChildren<Camera>();
                
                if (playerCamera != null)
                {
                    int cameraLayer = playerCamera.gameObject.layer;
                    
                    // Set canvas and all children to the camera's layer
                    healthCanvas.gameObject.layer = cameraLayer;
                    SetLayerRecursively(healthCanvas.gameObject, cameraLayer);
                    
                    // Set the world camera for event handling
                    healthCanvas.worldCamera = playerCamera;
                }
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