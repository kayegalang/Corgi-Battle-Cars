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
        private Vector3    direction;
        private float      currentScale = 1f;
        
        private void Start()
        {
            Destroy(gameObject, lifetime);
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
            transform.position += direction * speed * Time.deltaTime;
        }
        
        private void ExpandWave()
        {
            currentScale += expansionSpeed * Time.deltaTime;
            currentScale  = Mathf.Min(currentScale, maxScale);
            transform.localScale = Vector3.one * currentScale;
        }
        
        private void OnTriggerEnter(Collider other)
        {
            Debug.Log($"[BarkWave] Hit: {other.gameObject.name}");
            
            // Check both the collider's GameObject AND its root
            // so children of the owner (like CarModel, colliders etc.) are also ignored
            if (IsOwner(other))
            {
                Debug.Log($"[BarkWave] Ignoring owner");
                return;
            }
            
            CarController carController = other.GetComponent<CarController>()
                ?? other.transform.root.GetComponent<CarController>();

            BotController botController = other.GetComponent<BotController>()
                ?? other.transform.root.GetComponent<BotController>();

            Rigidbody targetRb = other.GetComponent<Rigidbody>()
                ?? other.transform.root.GetComponent<Rigidbody>();
            
            if ((carController != null || botController != null) && targetRb != null)
                PushBack(other.gameObject, targetRb);
        }

        private bool IsOwner(Collider other)
        {
            if (owner == null) return false;
            return other.gameObject == owner
                || other.transform.root.gameObject == owner;
        }
        
        private void PushBack(GameObject target, Rigidbody targetRb)
        {
            Vector3 pushDirection = (target.transform.position - transform.position).normalized;
            pushDirection.y = 0.3f;
            pushDirection.Normalize();
            
            Vector3 force = pushDirection * pushForce + Vector3.up * upwardForce;
            targetRb.AddForce(force, ForceMode.Impulse);

            target.GetComponent<CameraShaker>()?.ShakeBarkHit();
            
            Debug.Log($"[BarkWave] 💨 PUSHED {target.name} back! Force: {force}");
        }
    }
}