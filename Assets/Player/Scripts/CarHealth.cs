using System.Collections;
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

    void Start()
        
    {
        currentHealth = maxHealth;
        spawnManager = FindFirstObjectByType<SpawnManager>();
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
        spawnManager.Respawn(gameObject.tag);
        Destroy(gameObject);
    }

    public float GetHealthPercent()
    {
        return (float)currentHealth / maxHealth;
    }
}
