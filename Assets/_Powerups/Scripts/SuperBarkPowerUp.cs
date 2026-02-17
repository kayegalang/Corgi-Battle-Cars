using _Cars.Scripts;
using _PowerUps.Scripts;
using UnityEngine;

namespace _PowerUps.ScriptableObjects
{
    [CreateAssetMenu(fileName = "SuperBark", menuName = "Power Ups/Super Bark")]
    public class SuperBarkPowerUp : PowerUpObject
    {
        [Header("Super Bark Settings")]
        [Tooltip("Radius of the bark shockwave")]
        [Range(1f, 30f)]
        public float barkRadius = 10f;
        
        [Tooltip("Force applied to players caught in the shockwave")]
        [Range(1f, 50f)]
        public float knockbackForce = 20f;
        
        [Tooltip("Upward force component of the knockback")]
        [Range(0f, 20f)]
        public float knockbackUpForce = 5f;
        
        [Tooltip("Number of barks before the power-up is spent")]
        [Range(1, 5)]
        public int barkCount = 3;
        
        [Tooltip("Visual shockwave effect prefab")]
        public GameObject shockwaveEffectPrefab;
        
        [Tooltip("Sound played when barking")]
        public AudioClip barkSound;
        
        public override void Apply(GameObject player)
        {
            PowerUpHandler handler = player.GetComponent<PowerUpHandler>();
            
            if (handler == null)
            {
                Debug.LogWarning($"[SuperBarkPowerUp] No PowerUpHandler found on {player.name}!");
                return;
            }
            
            // Give the player their bark charges
            handler.SetBarkCharges(barkCount);
            handler.SetSuperBarkData(this);
            
            Debug.Log($"[SuperBarkPowerUp] Super Bark activated on {player.name}! {barkCount} barks ready");
        }
        
        public override void Remove(GameObject player)
        {
            if (player == null) return;
            
            PowerUpHandler handler = player.GetComponent<PowerUpHandler>();
            
            if (handler == null) return;
            
            handler.SetBarkCharges(0);
            handler.SetSuperBarkData(null);
            
            Debug.Log($"[SuperBarkPowerUp] Super Bark wore off on {player.name}");
        }
        
        /// <summary>
        /// Called by PowerUpHandler when the player uses a bark.
        /// </summary>
        public void ExecuteBark(GameObject player)
        {
            // Spawn shockwave visual
            if (shockwaveEffectPrefab != null)
            {
                GameObject effect = Object.Instantiate(shockwaveEffectPrefab, player.transform.position, Quaternion.identity);
                Object.Destroy(effect, 2f);
            }
            
            // Play bark sound
            if (barkSound != null)
            {
                AudioSource.PlayClipAtPoint(barkSound, player.transform.position);
            }
            
            // Find all players/bots in radius and knock them back
            Collider[] hits = Physics.OverlapSphere(player.transform.position, barkRadius);
            
            foreach (Collider hit in hits)
            {
                // Don't knock back the barker themselves
                if (hit.gameObject == player) continue;
                
                Rigidbody rb = hit.GetComponent<Rigidbody>();
                
                if (rb == null) continue;
                
                // Calculate direction away from barker
                Vector3 direction = (hit.transform.position - player.transform.position).normalized;
                direction.y = 0f;
                
                // Add upward component
                Vector3 force = (direction * knockbackForce) + (Vector3.up * knockbackUpForce);
                
                rb.AddForce(force, ForceMode.Impulse);
                
                Debug.Log($"[SuperBarkPowerUp] Knocked back {hit.gameObject.name}!");
            }
        }
    }
}
