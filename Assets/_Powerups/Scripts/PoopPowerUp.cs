using _PowerUps.Scripts;
using UnityEngine;

namespace _PowerUps.ScriptableObjects
{
    [CreateAssetMenu(fileName = "Poop", menuName = "Power Ups/Poop Trail")]
    public class PoopPowerUp : PowerUpObject
    {
        [Header("Poop Settings")]
        [Tooltip("How often (in seconds) a new poop pile is dropped")]
        [Range(0.1f, 2f)]
        public float dropInterval = 0.4f;
        
        [Tooltip("How long each poop pile stays in the world")]
        [Range(1f, 30f)]
        public float poopLifetime = 10f;
        
        [Tooltip("How long the spin-out lasts when a player hits poop")]
        [Range(0.5f, 5f)]
        public float spinOutDuration = 2f;
        
        [Tooltip("The poop prefab spawned in the world")]
        public GameObject poopPrefab;
        
        [Tooltip("Minimum speed the player must be going to drop poop")]
        [Range(0f, 10f)]
        public float minimumSpeedToDrop = 1f;
        
        [Tooltip("How far behind the car to spawn the poop")]
        [Range(1f, 10f)]
        public float spawnDistanceBehind = 4f;
        
        private float dropTimer = 0f;
        
        public override void Apply(GameObject player)
        {
            dropTimer = 0f;
            Debug.Log($"[PoopPowerUp] Poop trail activated on {player.name}! 💩");
        }
        
        public override void Remove(GameObject player)
        {
            dropTimer = 0f;
            Debug.Log($"[PoopPowerUp] Poop trail ended on {player.name}");
        }
        
        public override void OnUpdate(GameObject player)
        {
            if (player == null) return;
            
            dropTimer += Time.deltaTime;
            
            if (dropTimer >= dropInterval)
            {
                dropTimer = 0f;
                TryDropPoop(player);
            }
        }
        
        private void TryDropPoop(GameObject player)
        {
            // Only drop poop if the player is moving
            Rigidbody rb = player.GetComponent<Rigidbody>();
            if (rb != null && rb.linearVelocity.magnitude < minimumSpeedToDrop)
            {
                return;
            }
            
            if (poopPrefab == null)
            {
                Debug.LogWarning("[PoopPowerUp] No poop prefab assigned!");
                return;
            }
            
            // Drop poop BEHIND the player
            Vector3 spawnPos = player.transform.position - (player.transform.forward * spawnDistanceBehind);
            
            // Raycast down to place poop on the ground
            if (Physics.Raycast(spawnPos + Vector3.up * 2f, Vector3.down, out RaycastHit hit, 5f))
            {
                spawnPos = hit.point;
            }
            
            GameObject poop = Object.Instantiate(poopPrefab, spawnPos, Quaternion.Euler(0, Random.Range(0f, 360f), 0));
            
            // Set up the poop's spin-out data
            PoopHazard hazard = poop.GetComponent<PoopHazard>();
            if (hazard != null)
            {
                hazard.Initialize(player);
            }
            
            // Destroy poop after lifetime
            Object.Destroy(poop, poopLifetime);
        }
    }
}