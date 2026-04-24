using _Cars.Scripts;
using _Bot.Scripts;
using UnityEngine;

namespace _PowerUps.Scripts
{
    public class PoopHazard : MonoBehaviour
    {
        [Header("Slip Settings")]
        [Tooltip("How long the player loses control")]
        [SerializeField] private float slipDuration = 1.5f;
        
        [Tooltip("How strong the spin-out is")]
        [SerializeField] private float spinForce = 500f;
        
        [Header("Lifetime")]
        [Tooltip("How long before the poop disappears")]
        [SerializeField] private float lifetime = 10f;
        
        [Header("Grace Period")]
        [Tooltip("Time after spawning before owner can hit their own poop")]
        [SerializeField] private float ownerGracePeriod = 0.5f;
        
        private GameObject owner;
        private float spawnTime;
        
        private void Start()
        {
            Destroy(gameObject, lifetime);
            spawnTime = Time.time;
            Debug.Log($"[PoopHazard] Spawned at {transform.position}");
        }
        
        public void Initialize(GameObject ownerObject)
        {
            owner = ownerObject;
            Debug.Log($"[PoopHazard] Owner set to: {owner?.name}");
        }
        
        private void OnTriggerEnter(Collider other)
        {
            Debug.Log($"[PoopHazard] ★★★ OnTriggerEnter CALLED! ★★★");
            Debug.Log($"[PoopHazard] Collided with: {other.gameObject.name}");
            Debug.Log($"[PoopHazard] Other root: {other.transform.root.gameObject.name}");
            
            // Check if still in grace period for the owner
            if (owner != null && IsOwnerCollision(other))
            {
                float timeSinceSpawn = Time.time - spawnTime;
                if (timeSinceSpawn < ownerGracePeriod)
                {
                    Debug.Log($"[PoopHazard] Grace period active ({timeSinceSpawn:F2}s < {ownerGracePeriod}s) - ignoring owner");
                    return;
                }
                Debug.Log($"[PoopHazard] Grace period expired - owner CAN hit this poop!");
            }
            
            // Check if it's a car (player or bot)
            CarController carController = other.GetComponent<CarController>();
            BotController botController = other.GetComponent<BotController>();
            
            // Also check the root in case collider is on a child
            if (carController == null)
            {
                carController = other.transform.root.GetComponent<CarController>();
            }
            if (botController == null)
            {
                botController = other.transform.root.GetComponent<BotController>();
            }
            
            if (carController != null)
            {
                Debug.Log($"[PoopHazard] {other.gameObject.name} slipped on a poop! 💩");
                carController.TriggerSlip(slipDuration, spinForce);
                DestroyPoop();
            }
            else if (botController != null)
            {
                Debug.Log($"[PoopHazard] {other.gameObject.name} slipped on a poop! 💩");
                botController.TriggerSlip(slipDuration, spinForce);
                DestroyPoop();
            }
            else
            {
                Debug.LogWarning($"[PoopHazard] No CarController or BotController found on {other.gameObject.name} or its root!");
            }
        }
        
        private bool IsOwnerCollision(Collider other)
        {
            // Check both the collided object AND its root
            GameObject rootObject = other.transform.root.gameObject;
            
            bool directMatch = other.gameObject == owner;
            bool rootMatch = rootObject == owner;
            
            if (directMatch || rootMatch)
            {
                Debug.Log($"[PoopHazard] Owner match found! Direct: {directMatch}, Root: {rootMatch}");
                return true;
            }
            
            return false;
        }
        
        private void DestroyPoop()
        {
            Debug.Log($"[PoopHazard] Destroying poop!");
            Destroy(gameObject);
        }
    }
}