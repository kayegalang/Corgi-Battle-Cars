using _Bot.Scripts;
using _PowerUps.Scripts;
using UnityEngine;

namespace _PowerUps.ScriptableObjects
{
    [CreateAssetMenu(fileName = "Squirrel", menuName = "Power Ups/Squirrel")]
    public class SquirrelPowerUp : PowerUpObject
    {
        [Header("Squirrel Settings")]
        [Tooltip("How long bots/players are distracted by the squirrel")]
        [Range(1f, 15f)]
        public float distractDuration = 5f;
        
        [Tooltip("Radius within which the squirrel attracts enemies")]
        [Range(1f, 50f)]
        public float attractRadius = 20f;
        
        [Tooltip("The throwable squirrel prefab")]
        public GameObject squirrelProjectilePrefab;
        
        [Tooltip("Force used to throw the squirrel")]
        [Range(1f, 50f)]
        public float throwForce = 20f;
        
        public override void Apply(GameObject player)
        {
            PowerUpHandler handler = player.GetComponent<PowerUpHandler>();
            
            if (handler == null)
            {
                Debug.LogWarning($"[SquirrelPowerUp] No PowerUpHandler found on {player.name}!");
                return;
            }
            
            // Give the player a squirrel to throw
            handler.SetSquirrelData(this);
            handler.SetHasThrowable(true);
            
            Debug.Log($"[SquirrelPowerUp] Squirrel ready to throw on {player.name}!");
        }
        
        public override void Remove(GameObject player)
        {
            if (player == null) return;
            
            PowerUpHandler handler = player.GetComponent<PowerUpHandler>();
            
            if (handler == null) return;
            
            handler.SetSquirrelData(null);
            handler.SetHasThrowable(false);
            
            Debug.Log($"[SquirrelPowerUp] Squirrel power-up expired on {player.name}");
        }
        
        /// <summary>
        /// Called by PowerUpHandler when the player throws the squirrel.
        /// </summary>
        public void ThrowSquirrel(GameObject player, Vector3 throwDirection)
        {
            if (squirrelProjectilePrefab == null)
            {
                Debug.LogWarning("[SquirrelPowerUp] No squirrel projectile prefab assigned!");
                return;
            }
            
            // Spawn squirrel slightly in front of the player
            Vector3 spawnPos = player.transform.position + player.transform.forward * 1.5f + Vector3.up * 0.5f;
            
            GameObject squirrel = Object.Instantiate(squirrelProjectilePrefab, spawnPos, Quaternion.identity);
            
            // Set up the squirrel projectile
            SquirrelProjectile squirrelComp = squirrel.GetComponent<SquirrelProjectile>();
            if (squirrelComp != null)
            {
                squirrelComp.Initialize(player, attractRadius, distractDuration);
            }
            
            // Throw it
            Rigidbody rb = squirrel.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.AddForce(throwDirection * throwForce, ForceMode.Impulse);
            }
            
            Debug.Log($"[SquirrelPowerUp] Squirrel thrown by {player.name}!");
        }
    }
}
