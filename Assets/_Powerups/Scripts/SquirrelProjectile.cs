using System.Collections;
using System.Collections.Generic;
using _Audio.scripts;
using _Bot.Scripts;
using UnityEngine;

namespace _PowerUps.Scripts
{
    /// <summary>
    /// The thrown squirrel. When it lands, bots within the attract radius
    /// will abandon their current target and chase the squirrel instead.
    /// </summary>
    public class SquirrelProjectile : MonoBehaviour
    {
        private GameObject thrower;
        private float attractRadius;
        private float distractDuration;
        
        private bool hasLanded = false;
        private readonly List<BotAI> distractedBots = new List<BotAI>();
        
        [Header("Visuals")]
        [SerializeField] private GameObject landEffectPrefab;
        
        public void Initialize(GameObject projectileThrower, float radius, float duration)
        {
            thrower         = projectileThrower;
            attractRadius   = radius;
            distractDuration = duration;

            // Play squirrel launch sound
            if (AudioManager.instance != null && FMODEvents.instance != null)
                AudioManager.instance.PlayOneShot(FMODEvents.instance.squirrel, transform.position);
        }
        
        private void OnCollisionEnter(Collision collision)
        {
            // Only react to the first collision (landing)
            if (hasLanded) return;
            
            hasLanded = true;
            
            // Spawn land effect
            if (landEffectPrefab != null)
            {
                GameObject effect = Instantiate(landEffectPrefab, transform.position, Quaternion.identity);
                Destroy(effect, 2f);
            }
            
            // Freeze the squirrel in place
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.isKinematic = true;
            }
            
            // Attract nearby bots
            StartCoroutine(AttractBots());
        }
        
        private IEnumerator AttractBots()
        {
            Debug.Log($"[SquirrelProjectile] Squirrel landed! Attracting bots within {attractRadius}m");
            
            // Find all bots in radius
            Collider[] hits = Physics.OverlapSphere(transform.position, attractRadius);
            
            foreach (Collider hit in hits)
            {
                BotAI bot = hit.GetComponent<BotAI>();
                
                // Don't distract the thrower's team (if we wanted teams later)
                if (bot != null && hit.gameObject != thrower)
                {
                    //bot.SetSquirrelTarget(transform);
                    distractedBots.Add(bot);
                    Debug.Log($"[SquirrelProjectile] {hit.gameObject.name} is chasing the squirrel! 🐿️");
                }
            }
            
            // Wait for distract duration
            yield return new WaitForSeconds(distractDuration);
            
            // Release all bots back to normal behavior
            foreach (BotAI bot in distractedBots)
            {
                if (bot != null)
                {
                    //bot.ClearSquirrelTarget();
                }
            }
            
            distractedBots.Clear();
            
            Debug.Log("[SquirrelProjectile] Bots lost interest in the squirrel");
            
            Destroy(gameObject);
        }
    }
}
