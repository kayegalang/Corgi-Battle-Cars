using System.Collections;
using BotScript;
using Gameplay.Scripts;
using Player.Scripts;
using UnityEngine;
using UnityEngine.Rendering;

public class CarHealth : MonoBehaviour
{
    [SerializeField] private SpawnManager spawnManager;
    [SerializeField] HealthBar healthBar;
    private int maxHealth = 100;
    private int currentHealth;

    private bool isBot;

    void Start()
        
    {
        currentHealth = maxHealth;
        spawnManager = FindFirstObjectByType<SpawnManager>();
        isBot = GetComponent<BotAI>() != null;
    }

    public void TakeDamage(int amount)
    {
        currentHealth -= amount;
        healthBar.UpdateHealthBar();
        
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        spawnManager.Spawn(gameObject.tag);
        Destroy(gameObject);
    }

    public float GetHealthPercent()
    {
        return (float)currentHealth / maxHealth;
    }
}
