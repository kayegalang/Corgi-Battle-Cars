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

        [Header("Ground Snap")]
        [Tooltip("Layers to raycast against when snapping to ground")]
        [SerializeField] private LayerMask groundMask;
        [Tooltip("How far above the hit point to place the poop")]
        [SerializeField] private float groundOffset = 0.05f;
        [Tooltip("Maximum distance to search downward for ground")]
        [SerializeField] private float groundCheckDistance = 20f;
        
        private GameObject owner;
        private float spawnTime;
        
        private void Start()
        {
            SnapToGround();
            Destroy(gameObject, lifetime);
            spawnTime = Time.time;
            Debug.Log($"[PoopHazard] Spawned at {transform.position}");
        }

        // ═══════════════════════════════════════════════
        //  GROUND SNAP
        // ═══════════════════════════════════════════════

        private void SnapToGround()
        {
            // Cast downward from slightly above the spawn position
            Vector3 origin = transform.position + Vector3.up * 0.5f;

            if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit,
                groundCheckDistance, groundMask, QueryTriggerInteraction.Ignore))
            {
                transform.position = hit.point + Vector3.up * groundOffset;
                // Align rotation to ground normal so it sits flat on slopes
                transform.rotation = Quaternion.FromToRotation(Vector3.up, hit.normal);
                Debug.Log($"[PoopHazard] Snapped to ground at {transform.position}");
            }
            else
            {
                Debug.LogWarning($"[PoopHazard] No ground found below spawn point — poop may float!");
            }
        }
        
        // ═══════════════════════════════════════════════
        //  INITIALIZE
        // ═══════════════════════════════════════════════

        public void Initialize(GameObject ownerObject)
        {
            owner = ownerObject;
            Debug.Log($"[PoopHazard] Owner set to: {owner?.name}");
        }
        
        // ═══════════════════════════════════════════════
        //  COLLISION
        // ═══════════════════════════════════════════════

        private void OnTriggerEnter(Collider other)
        {
            Debug.Log($"[PoopHazard] ★★★ OnTriggerEnter CALLED! ★★★");
            Debug.Log($"[PoopHazard] Collided with: {other.gameObject.name}");
            Debug.Log($"[PoopHazard] Other root: {other.transform.root.gameObject.name}");
            
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
            
            CarController carController = other.GetComponent<CarController>();
            BotController botController = other.GetComponent<BotController>();
            
            if (carController == null)
                carController = other.transform.root.GetComponent<CarController>();
            if (botController == null)
                botController = other.transform.root.GetComponent<BotController>();
            
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
            GameObject rootObject = other.transform.root.gameObject;
            bool directMatch      = other.gameObject == owner;
            bool rootMatch        = rootObject == owner;
            
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