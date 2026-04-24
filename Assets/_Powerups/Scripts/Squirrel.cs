using _Cars.Scripts;
using _Bot.Scripts;
using System.Collections.Generic;
using UnityEngine;

namespace _PowerUps.Scripts
{
    public class Squirrel : MonoBehaviour
    {
        [Header("Chase Settings")]
        [Tooltip("How far away cars will be attracted to the squirrel")]
        [SerializeField] private float chaseRadius = 15f;
        
        [Tooltip("How long the squirrel lasts before disappearing")]
        [SerializeField] private float lifetime = 10f;
        
        [Header("Movement Settings")]
        [Tooltip("How fast the squirrel runs on the ground")]
        [SerializeField] private float runSpeed = 8f;
        
        [Tooltip("How fast charmed cars turn toward the squirrel")]
        [SerializeField] private float turnSpeed = 3f;
        
        [Tooltip("How fast charmed cars accelerate toward the squirrel")]
        [SerializeField] private float moveSpeed = 1f;
        
        private GameObject owner;
        private List<GameObject> charmedCars = new List<GameObject>();
        private bool hasLanded = false;
        private Vector3 runDirection;
        private Rigidbody rb;
        
        private void Start()
        {
            rb = GetComponent<Rigidbody>();
            
            // Store the initial throw direction (forward direction when spawned)
            runDirection = transform.forward;
            
            // Destroy after lifetime
            Destroy(gameObject, lifetime);
        }
        
        private void Update()
        {
            // Only start affecting cars after we've landed
            if (!hasLanded) return;
            
            UpdateCharmedCars();
            ControlCharmedCars();
        }
        
        private void FixedUpdate()
        {
            // Run on the ground after landing!
            if (hasLanded && rb != null)
            {
                RunOnGround();
            }
        }
        
        private void OnCollisionEnter(Collision collision)
        {
            // Once we hit the ground, we're ready to run and charm cars
            if (!hasLanded)
            {
                hasLanded = true;
            }
        }
        
        private void RunOnGround()
        {
            // Cast a ray down to find the ground
            RaycastHit hit;
            float rayDistance = 10f;
            
            if (Physics.Raycast(transform.position, Vector3.down, out hit, rayDistance))
            {
                // Position squirrel slightly above the ground
                float hoverHeight = 0.3f; // Adjust based on your squirrel's size
                Vector3 targetPosition = hit.point + Vector3.up * hoverHeight;
                
                // Move horizontally in the run direction
                Vector3 horizontalMovement = runDirection * runSpeed * Time.fixedDeltaTime;
                targetPosition += horizontalMovement;
                
                // Smoothly move to target position
                rb.MovePosition(targetPosition);
                
                // Rotate to face the run direction
                if (runDirection != Vector3.zero)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(runDirection);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 10f * Time.fixedDeltaTime);
                }
            }
            else
            {
                // If no ground detected, just move horizontally and let gravity handle it
                Vector3 horizontalVelocity = new Vector3(
                    runDirection.x * runSpeed,
                    rb.linearVelocity.y, // Keep current Y velocity (gravity)
                    runDirection.z * runSpeed
                );
                rb.linearVelocity = horizontalVelocity;
            }
        }
        
        public void Initialize(GameObject ownerObject)
        {
            owner = ownerObject;
        }
        
        private void UpdateCharmedCars()
        {
            // Find all cars within chase radius
            Collider[] hitColliders = Physics.OverlapSphere(transform.position, chaseRadius);
            
            // Clear the list and rebuild it
            List<GameObject> newCharmedCars = new List<GameObject>();
            
            foreach (Collider col in hitColliders)
            {
                GameObject car = col.gameObject;
                
                // Check if it's a car
                CarController carController = car.GetComponent<CarController>();
                BotController botController = car.GetComponent<BotController>();
                
                // Also check root in case collider is on child
                if (carController == null && botController == null)
                {
                    carController = car.transform.root.GetComponent<CarController>();
                    botController = car.transform.root.GetComponent<BotController>();
                    car = car.transform.root.gameObject;
                }
                
                // Don't affect the owner (check AFTER we've found the actual car root)
                if (owner != null && car == owner)
                {
                    continue;
                }
                
                if (carController != null || botController != null)
                {
                    if (!newCharmedCars.Contains(car))
                    {
                        newCharmedCars.Add(car);
                    }
                }
            }
            
            // Release cars that left the radius
            foreach (GameObject car in charmedCars)
            {
                if (!newCharmedCars.Contains(car))
                {
                    ReleaseControl(car);
                }
            }
            
            charmedCars = newCharmedCars;
        }
        
        private void ControlCharmedCars()
        {
            foreach (GameObject car in charmedCars)
            {
                if (car == null) continue;
                
                // Calculate direction to squirrel
                Vector3 directionToSquirrel = (transform.position - car.transform.position).normalized;
                
                // Get the car's controller
                CarController carController = car.GetComponent<CarController>();
                BotController botController = car.GetComponent<BotController>();
                
                if (carController != null)
                {
                    CharmCarController(carController, directionToSquirrel);
                }
                else if (botController != null)
                {
                    CharmBotController(botController, directionToSquirrel);
                }
            }
        }
        
        private void CharmCarController(CarController carController, Vector3 directionToSquirrel)
        {
            // Calculate turn input (-1 to 1)
            float angle = Vector3.SignedAngle(carController.transform.forward, directionToSquirrel, Vector3.up);
            float turnInput = Mathf.Clamp(angle / 45f, -1f, 1f) * turnSpeed;
            
            // Always move forward toward squirrel
            float moveInput = moveSpeed;
            
            // Override their inputs!
            carController.SetInputs(turnInput, moveInput);
        }
        
        private void CharmBotController(BotController botController, Vector3 directionToSquirrel)
        {
            // Calculate turn input (-1 to 1)
            float angle = Vector3.SignedAngle(botController.transform.forward, directionToSquirrel, Vector3.up);
            float turnInput = Mathf.Clamp(angle / 45f, -1f, 1f) * turnSpeed;
            
            // Always move forward toward squirrel
            float moveInput = moveSpeed;
            
            // Override their inputs!
            botController.SetInputs(turnInput, moveInput);
        }
        
        private void ReleaseControl(GameObject car)
        {
            if (car == null) return;
            
            // Reset inputs to neutral
            CarController carController = car.GetComponent<CarController>();
            BotController botController = car.GetComponent<BotController>();
            
            if (carController != null)
            {
                // Player controller will be overridden by player input anyway
            }
            else if (botController != null)
            {
                // Bot will resume normal AI behavior
            }
        }
        
        private void OnDestroy()
        {
            // Release all charmed cars when squirrel despawns
            foreach (GameObject car in charmedCars)
            {
                ReleaseControl(car);
            }
            
            charmedCars.Clear();
        }
        
        private void OnDrawGizmosSelected()
        {
            // Show the chase radius in the editor
            Gizmos.color = new Color(0.6f, 0.4f, 0.2f, 0.3f); // Brown transparent
            Gizmos.DrawSphere(transform.position, chaseRadius);
            
            Gizmos.color = new Color(0.6f, 0.4f, 0.2f, 1f); // Brown solid
            Gizmos.DrawWireSphere(transform.position, chaseRadius);
            
            // Show the run direction
            if (hasLanded)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawRay(transform.position, runDirection * 3f);
            }
        }
    }
}