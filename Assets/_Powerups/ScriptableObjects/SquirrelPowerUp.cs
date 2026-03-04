using _PowerUps.Scripts;
using UnityEngine;

namespace _PowerUps.ScriptableObjects
{
    [CreateAssetMenu(fileName = "Squirrel", menuName = "Power Ups/Squirrel")]
    public class SquirrelPowerUp : PowerUpObject
    {
        [Header("Squirrel Settings")]
        [Tooltip("The squirrel prefab that gets thrown")]
        public GameObject squirrelPrefab;
        
        [Tooltip("How hard the squirrel is thrown")]
        [Range(10f, 50f)]
        public float throwForce = 25f;
        
        [Tooltip("Upward force component for arc")]
        [Range(1f, 20f)]
        public float upwardForce = 8f;
        
        [Header("Audio")]
        public AudioClip throwSound;
        
        public override void Apply(GameObject player)
        {
            // Squirrel is instant-use, so we just throw it immediately
            ThrowSquirrel(player, player.transform.forward);
            
            Debug.Log($"[SquirrelPowerUp] {player.name} threw a squirrel! 🐿️");
        }
        
        public override void Remove(GameObject player)
        {
            // Nothing to remove - squirrel is already in the world
        }
        
        public override void OnUpdate(GameObject player)
        {
            // No per-frame updates needed
        }
        
        /// <summary>
        /// Throws the squirrel in the specified direction
        /// </summary>
        public void ThrowSquirrel(GameObject player, Vector3 direction)
        {
            if (squirrelPrefab == null)
            {
                Debug.LogError("[SquirrelPowerUp] No squirrel prefab assigned!");
                return;
            }
            
            // Spawn squirrel slightly in front of and above the player
            Vector3 spawnPosition = player.transform.position + 
                                   player.transform.forward * 2f + 
                                   Vector3.up * 1f;
            
            // Make squirrel face the same direction as the player!
            Quaternion spawnRotation = Quaternion.LookRotation(direction);
            
            GameObject squirrel = Instantiate(squirrelPrefab, spawnPosition, spawnRotation);
            
            // Set the owner so it doesn't affect the thrower
            Squirrel squirrelScript = squirrel.GetComponent<Squirrel>();
            if (squirrelScript != null)
            {
                squirrelScript.Initialize(player);
            }
            
            // Throw it with physics!
            Rigidbody squirrelRb = squirrel.GetComponent<Rigidbody>();
            if (squirrelRb != null)
            {
                Vector3 throwDirection = direction.normalized;
                Vector3 force = throwDirection * throwForce + Vector3.up * upwardForce;
                squirrelRb.AddForce(force, ForceMode.Impulse);
            }
            
            // Play throw sound
            if (throwSound != null)
            {
                AudioSource.PlayClipAtPoint(throwSound, player.transform.position);
            }
            
            Debug.Log($"[SquirrelPowerUp] Squirrel launched in direction {direction}! 🐿️💨");
        }
    }
}