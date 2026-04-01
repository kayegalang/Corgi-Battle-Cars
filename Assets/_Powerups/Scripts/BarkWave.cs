using _Cars.Scripts;
using _Bot.Scripts;
using UnityEngine;

namespace _PowerUps.Scripts
{
    public class BarkWave : MonoBehaviour
    {
        [Header("Wave Settings")]
        [Tooltip("How fast the wave travels forward")]
        [SerializeField] private float speed = 20f;
        
        [Tooltip("How fast the wave expands (scales up)")]
        [SerializeField] private float expansionSpeed = 3f;
        
        [Tooltip("Maximum scale the wave can reach")]
        [SerializeField] private float maxScale = 5f;
        
        [Tooltip("How long before the wave disappears")]
        [SerializeField] private float lifetime = 2f;
        
        [Header("Push Settings")]
        [Tooltip("How hard enemies get pushed back")]
        [SerializeField] private float pushForce = 1500f;
        
        [Tooltip("Upward force component (for launching enemies)")]
        [SerializeField] private float upwardForce = 300f;
        
        private GameObject owner;
        private Vector3 direction;
        private float currentScale = 1f;
        
        private void Start()
        {
            // Destroy after lifetime
            Destroy(gameObject, lifetime);
            
            // Set initial direction (forward)
            direction = transform.forward;
            
            Debug.Log($"[BarkWave] Sound wave spawned! Direction: {direction}");
        }
        
        private void Update()
        {
            MoveForward();
            ExpandWave();
        }
        
        public void Initialize(GameObject ownerObject)
        {
            owner = ownerObject;
            Debug.Log($"[BarkWave] Owner set to: {owner.name}");
        }
        
        private void MoveForward()
        {
            // Move in the direction it was spawned
            transform.position += direction * speed * Time.deltaTime;
        }
        
        private void ExpandWave()
        {
            // Gradually scale up
            currentScale += expansionSpeed * Time.deltaTime;
            currentScale = Mathf.Min(currentScale, maxScale);
            
            transform.localScale = Vector3.one * currentScale;
        }
        
        private void OnTriggerEnter(Collider other)
        {
            Debug.Log($"[BarkWave] Hit: {other.gameObject.name}");
            
            // Don't affect the owner
            if (owner != null && other.gameObject == owner)
            {
                Debug.Log($"[BarkWave] Ignoring owner");
                return;
            }
            
            // Check if it's a car (player or bot)
            CarController carController = other.GetComponent<CarController>();
            BotController botController = other.GetComponent<BotController>();
            
            // Also check root in case collider is on a child
            if (carController == null)
            {
                carController = other.transform.root.GetComponent<CarController>();
            }
            if (botController == null)
            {
                botController = other.transform.root.GetComponent<BotController>();
            }
            
            // Get the rigidbody
            Rigidbody targetRb = other.GetComponent<Rigidbody>();
            if (targetRb == null)
            {
                targetRb = other.transform.root.GetComponent<Rigidbody>();
            }
            
            if ((carController != null || botController != null) && targetRb != null)
            {
                PushBack(other.gameObject, targetRb);
            }
        }
        
        private void PushBack(GameObject target, Rigidbody targetRb)
        {
            // Calculate push direction (away from the wave's origin)
            Vector3 pushDirection = (target.transform.position - transform.position).normalized;
            
            // Add upward component for dramatic effect
            pushDirection.y = 0.3f; // Mix in some upward force
            pushDirection.Normalize();
            
            // Apply the force
            Vector3 force = pushDirection * pushForce + Vector3.up * upwardForce;
            targetRb.AddForce(force, ForceMode.Impulse);

            // Shake the hit target's camera
            target.GetComponent<CameraShaker>()?.ShakeBarkHit();
            
            Debug.Log($"[BarkWave] 💨 PUSHED {target.name} back! Force: {force}");
        }
    }
}