using UnityEngine;

public class CarHealth : MonoBehaviour
{
    private int maxHealth = 100;
    private int currentHealth;

    void Start()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(int amount)
    {
        currentHealth -= amount;
        
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        Debug.Log("Car Died!");
        // TODO: Explode car, respawn
    }
    
    public float GetHealthPercent()
    {
        return (float)currentHealth / maxHealth;
    }
    
}
