using _PowerUps.Scripts;
using UnityEngine;

namespace _PowerUps.ScriptableObjects
{
    [CreateAssetMenu(fileName = "SuperBark", menuName = "Power Ups/Super Bark")]
    public class SuperBarkPowerUp : PowerUpObject
    {
        [Header("Bark Settings")]
        [Tooltip("Time between each bark")]
        [Range(0.1f, 3f)]
        public float barkInterval = 0.8f;
        
        [Tooltip("The bark wave prefab that gets spawned")]
        public GameObject barkWavePrefab;
        
        [Tooltip("How far in front of the car to spawn the wave")]
        public float spawnDistance = 2f;
        
        [Header("Audio")]
        public AudioClip barkSound;
        
        // Track next bark time per player
        private float nextBarkTime = 0f;
        
        public override void Apply(GameObject player)
        {
            // Reset bark timer when power-up activates
            nextBarkTime = Time.time;
            
            Debug.Log($"[SuperBarkPowerUp] {player.name} got Super Bark! Auto-barking for {duration} seconds!");
        }
        
        public override void Remove(GameObject player)
        {
            Debug.Log($"[SuperBarkPowerUp] {player.name}'s Super Bark expired!");
        }
        
        public override void OnUpdate(GameObject player)
        {
            // Auto-bark at intervals!
            if (Time.time >= nextBarkTime)
            {
                ExecuteBark(player);
                nextBarkTime = Time.time + barkInterval;
            }
        }
        
        /// <summary>
        /// Spawns a bark wave
        /// </summary>
        public void ExecuteBark(GameObject player)
        {
            if (barkWavePrefab == null)
            {
                Debug.LogError("[SuperBarkPowerUp] No bark wave prefab assigned!");
                return;
            }
            
            // Spawn the bark wave in front of the player
            Vector3 spawnPosition = player.transform.position + player.transform.forward * spawnDistance;
            Quaternion spawnRotation = player.transform.rotation;
            
            GameObject wave = Instantiate(barkWavePrefab, spawnPosition, spawnRotation);
            
            // Set the wave's owner so it doesn't affect the player
            BarkWave barkWave = wave.GetComponent<BarkWave>();
            if (barkWave != null)
            {
                barkWave.Initialize(player);
            }
            
            // Play bark sound
            if (barkSound != null)
            {
                AudioSource.PlayClipAtPoint(barkSound, player.transform.position);
            }
            
            Debug.Log($"[SuperBarkPowerUp] {player.name} AUTO-BARKED! 🐕💨");
        }
    }
}