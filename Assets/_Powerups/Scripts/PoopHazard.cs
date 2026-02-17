using System.Collections;
using _Cars.Scripts;
using UnityEngine;

namespace _PowerUps.Scripts
{
    /// <summary>
    /// Placed in the world by the Poop power-up.
    /// Causes players that drive through it to spin out.
    /// </summary>
    public class PoopHazard : MonoBehaviour
    {
        private GameObject owner;
        private float spinOutDuration;
        private bool isInitialized = false;
        
        [Header("Spin Out Settings")]
        [SerializeField] private float spinTorque = 15f;
        [SerializeField] private float speedReduction = 0.3f; // Multiplied against current velocity
        
        public void Initialize(GameObject poopOwner, float spinDuration)
        {
            owner = poopOwner;
            spinOutDuration = spinDuration;
            isInitialized = true;
        }
        
        private void OnTriggerEnter(Collider other)
        {
            if (!isInitialized) return;
            
            // Don't spin out the person who dropped the poop
            if (other.gameObject == owner) return;
            
            // Only affect players and bots (anything with a Rigidbody and CarHealth)
            CarHealth carHealth = other.GetComponent<CarHealth>();
            Rigidbody rb = other.GetComponent<Rigidbody>();
            
            if (carHealth == null || rb == null) return;
            
            StartCoroutine(SpinOut(other.gameObject, rb));
        }
        
        private IEnumerator SpinOut(GameObject player, Rigidbody rb)
        {
            Debug.Log($"[PoopHazard] {player.name} stepped in poop! 💩");
            
            // Disable player controls during spin
            CarController carController = player.GetComponent<CarController>();
            bool wasEnabled = false;
            
            if (carController != null)
            {
                wasEnabled = carController.enabled;
                carController.enabled = false;
            }
            
            // Slow them down
            rb.linearVelocity *= speedReduction;
            
            float elapsed = 0f;
            float direction = Random.value > 0.5f ? 1f : -1f; // Random spin direction
            
            while (elapsed < spinOutDuration)
            {
                // Apply spinning torque
                rb.AddTorque(Vector3.up * spinTorque * direction, ForceMode.Force);
                
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            // Restore controls
            if (carController != null)
            {
                carController.enabled = wasEnabled;
            }
            
            Debug.Log($"[PoopHazard] {player.name} recovered from poop spin-out");
        }
    }
}
