using _Bot.Scripts;
using _Gameplay.Scripts;
using _UI.Scripts;
using UnityEngine;
using UnityEngine.InputSystem;

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
            
            if (spawnManager == null)
                spawnManager = FindFirstObjectByType<SpawnManager>();
            
            if (spawnManager == null)
                Debug.LogWarning($"SpawnManager not found for {gameObject.name}");
        }

        public void TakeDamage(int amount, GameObject shooter)
        {
            currentHealth -= amount;
            
            if (healthBar != null)
                healthBar.UpdateHealthBar();
        
            if (currentHealth <= 0)
            {
                Die(shooter);
            }

            if (isBot)
            {
                var botAI = GetComponent<BotAI>();
                if (botAI != null && shooter != null)
                {
                    botAI.OnHit(shooter.transform);
                }
            }
        }

        private void Die(GameObject shooter)
        {
            if (isDead) return;
            isDead = true;

            if (shooter != null)
            {
                PointsManager.instance.AddPoint(shooter.tag);
            }
            else
            {
                Debug.LogWarning($"{gameObject.name} died but shooter is null");
            }

            if (spawnManager == null)
            {
                spawnManager = FindFirstObjectByType<SpawnManager>();
            }

            // Get the root GameObject (which should have the Player tag)
            GameObject rootObject = transform.root.gameObject;

            // Check if this is a player
            bool isPlayer = rootObject.CompareTag("Player1") || rootObject.CompareTag("Player2") ||
                            rootObject.CompareTag("Player3") || rootObject.CompareTag("Player4");

            if (isPlayer)
            {
                // IMMEDIATELY disable physics and teleport far away
                var rb = rootObject.GetComponentInChildren<Rigidbody>(true);
                if (rb != null)
                {
                    rb.isKinematic = true;
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }

                // Teleport WAY below the map so it's not visible/targetable
                rootObject.transform.position = new Vector3(0, -1000, 0);

                // Find the "Player" child GameObject and deactivate it
                Transform playerChild = rootObject.transform.Find("Player");
                if (playerChild != null)
                {
                    playerChild.gameObject.SetActive(false);
                    Debug.Log($"Deactivated Player child of {rootObject.name}");
                }
                else
                {
                    Debug.LogWarning($"Could not find 'Player' child on {rootObject.name}");
                }

                // Start respawn coroutine
                if (spawnManager != null)
                {
                    spawnManager.Respawn(rootObject.tag);
                }
            }
            else
            {
                // For bots, destroy and respawn with new instance
                if (spawnManager != null)
                {
                    spawnManager.Respawn(rootObject.tag);
                }

                Destroy(rootObject);
            }
        }

        public float GetHealthPercent()
        {
            return (float)currentHealth / maxHealth;
        }

        public bool GetIsBot()
        {
            return isBot;
        }
        
        public void ResetHealth()
        {
            currentHealth = maxHealth;
            isDead = false;
            
            if (healthBar != null)
                healthBar.UpdateHealthBar();
            
            Debug.Log($"{gameObject.name} health reset to {maxHealth}");
        }
    }
}