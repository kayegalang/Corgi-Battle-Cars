using UnityEngine;

namespace _Cars.ScriptableObjects
{
    [CreateAssetMenu(fileName = "CarStats", menuName = "Scriptable Objects/CarStats")]
    public class CarStats : ScriptableObject
    {
        [Header("Movement")]
        [Tooltip("How quickly the car accelerates")]
        [SerializeField] [Range(1f, 100f)] private float acceleration = 20f;
        
        [Tooltip("How quickly the car turns")]
        [SerializeField] [Range(1f, 100f)] private float turnSpeed = 20f;
        
        [Tooltip("Maximum speed the car can reach")]
        [SerializeField] [Range(1f, 100f)] private float maxSpeed = 35f;
        
        [Header("Jumping")]
        [Tooltip("The force applied when jumping")]
        [SerializeField] [Range(1f, 20f)] private float jumpForce = 5f;
        
        [Header("Ground Detection")]
        [Tooltip("Offset from car center for ground check raycast")]
        [SerializeField] private Vector3 groundCheckOffset = new Vector3(0f, 0.26f, 0f);
        
        [Tooltip("Distance of ground check raycast")]
        [SerializeField] [Range(0.1f, 2f)] private float groundCheckDistance = 0.26f;
        
        public float Acceleration => acceleration;
        public float TurnSpeed => turnSpeed;
        public float MaxSpeed => maxSpeed;
        public float JumpForce => jumpForce;
        public Vector3 GroundCheckOffset => groundCheckOffset;
        public float GroundCheckDistance => groundCheckDistance;
        
        private void OnValidate()
        {
            ValidateAcceleration();
            ValidateTurnSpeed();
            ValidateMaxSpeed();
            ValidateJumpForce();
            ValidateGroundCheckDistance();
        }
        
        private void ValidateAcceleration()
        {
            if (acceleration <= 0f)
            {
                Debug.LogWarning($"[{nameof(CarStats)}] Acceleration should be greater than 0 on {name}!", this);
                acceleration = 20f;
            }
        }
        
        private void ValidateTurnSpeed()
        {
            if (turnSpeed <= 0f)
            {
                Debug.LogWarning($"[{nameof(CarStats)}] Turn speed should be greater than 0 on {name}!", this);
                turnSpeed = 20f;
            }
        }
        
        private void ValidateMaxSpeed()
        {
            if (maxSpeed <= 0f)
            {
                Debug.LogWarning($"[{nameof(CarStats)}] Max speed should be greater than 0 on {name}!", this);
                maxSpeed = 35f;
            }
        }
        
        private void ValidateJumpForce()
        {
            if (jumpForce <= 0f)
            {
                Debug.LogWarning($"[{nameof(CarStats)}] Jump force should be greater than 0 on {name}!", this);
                jumpForce = 5f;
            }
        }
        
        private void ValidateGroundCheckDistance()
        {
            if (groundCheckDistance <= 0f)
            {
                Debug.LogWarning($"[{nameof(CarStats)}] Ground check distance should be greater than 0 on {name}!", this);
                groundCheckDistance = 0.26f;
            }
        }
    }
}