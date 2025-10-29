using _Bot.Scripts;
using _Gameplay.Scripts;
using _UI.Scripts;
using Gameplay.Scripts;
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
        }

        public void TakeDamage(int amount, GameObject shooter)
        {
            currentHealth -= amount;
            healthBar.UpdateHealthBar();
        
            if (currentHealth <= 0)
            {
                Die();
            }

            if (isBot)
            {
                GetComponent<BotAI>().OnHit(shooter.transform);
            }
        }

        private void Die()
        {
            if (isDead) return;
            isDead = true;
            
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
