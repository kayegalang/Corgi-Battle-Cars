using UnityEngine;

namespace _Cars.ScriptableObjects
{
    [CreateAssetMenu(fileName = "CarStats", menuName = "Scriptable Objects/CarStats")]
    public class CarStats : ScriptableObject
    {
        [Header("Car Stats (0-100%)")]
        [Tooltip("Speed stat (affects max speed and turning)")]
        [SerializeField] [Range(0f, 100f)] private float speedStat = 50f;
        
        [Tooltip("Acceleration stat (affects how quickly car reaches max speed)")]
        [SerializeField] [Range(0f, 100f)] private float accelerationStat = 50f;
        
        [Tooltip("Jump Force stat (affects jump height)")]
        [SerializeField] [Range(0f, 100f)] private float jumpForceStat = 50f;
        
        [Tooltip("Health stat (affects max health)")]
        [SerializeField] [Range(0f, 100f)] private float healthStat = 50f;
        
        [Header("Speed Range")]
        [Tooltip("Minimum max speed (at 0%)")]
        [SerializeField] private float minMaxSpeed = 25f;
        
        [Tooltip("Maximum max speed (at 100%)")]
        [SerializeField] private float maxMaxSpeed = 50f;
        
        [Header("Acceleration Range")]
        [Tooltip("Minimum acceleration force (at 0%)")]
        [SerializeField] private float minAcceleration = 12f;
        
        [Tooltip("Maximum acceleration force (at 100%)")]
        [SerializeField] private float maxAcceleration = 30f;
        
        [Header("Jump Force Range")]
        [Tooltip("Minimum jump force (at 0%)")]
        [SerializeField] private float minJumpForce = 8f;
        
        [Tooltip("Maximum jump force (at 100%)")]
        [SerializeField] private float maxJumpForce = 14f;
        
        [Header("Health Range")]
        [Tooltip("Minimum max health (at 0%)")]
        [SerializeField] private int minMaxHealth = 75;
        
        [Tooltip("Maximum max health (at 100%)")]
        [SerializeField] private int maxMaxHealth = 125;
        
        [Header("Turn Speed Range")]
        [Tooltip("Minimum turn speed (at 0% speed)")]
        [SerializeField] private float minTurnSpeed = 20f;
        
        [Tooltip("Maximum turn speed (at 100% speed)")]
        [SerializeField] private float maxTurnSpeed = 35f;
        
        [Header("Ground Detection")]
        [Tooltip("Offset from car center for ground check raycast")]
        [SerializeField] private Vector3 groundCheckOffset = new Vector3(0f, 0.26f, 0f);
        
        [Tooltip("Distance of ground check raycast")]
        [SerializeField] [Range(0.1f, 2f)] private float groundCheckDistance = 0.26f;
        
        // Calculated properties that CarController uses
        public float Acceleration => CalculateAcceleration();
        public float TurnSpeed => CalculateTurnSpeed();
        public float MaxSpeed => CalculateMaxSpeed();
        public float JumpForce => CalculateJumpForce();
        public int MaxHealth => CalculateMaxHealth();
        public Vector3 GroundCheckOffset => groundCheckOffset;
        public float GroundCheckDistance => groundCheckDistance;
        
        // Stat getters for UI (returns 0-100)
        public float SpeedStat => speedStat;
        public float AccelerationStat => accelerationStat;
        public float JumpForceStat => jumpForceStat;
        public float HealthStat => healthStat;
        
        private float CalculateMaxSpeed()
        {
            return Mathf.Lerp(minMaxSpeed, maxMaxSpeed, speedStat / 100f);
        }
        
        private float CalculateAcceleration()
        {
            return Mathf.Lerp(minAcceleration, maxAcceleration, accelerationStat / 100f);
        }
        
        private float CalculateJumpForce()
        {
            return Mathf.Lerp(minJumpForce, maxJumpForce, jumpForceStat / 100f);
        }
        
        private int CalculateMaxHealth()
        {
            return Mathf.RoundToInt(Mathf.Lerp(minMaxHealth, maxMaxHealth, healthStat / 100f));
        }
        
        private float CalculateTurnSpeed()
        {
            return Mathf.Lerp(minTurnSpeed, maxTurnSpeed, speedStat / 100f);
        }
        
        private void OnValidate()
        {
            ValidateRanges();
        }
        
        private void ValidateRanges()
        {
            if (minMaxSpeed >= maxMaxSpeed)
            {
                Debug.LogWarning($"[{nameof(CarStats)}] Min max speed must be less than max max speed on {name}!", this);
            }
            
            if (minAcceleration >= maxAcceleration)
            {
                Debug.LogWarning($"[{nameof(CarStats)}] Min acceleration must be less than max acceleration on {name}!", this);
            }
            
            if (minJumpForce >= maxJumpForce)
            {
                Debug.LogWarning($"[{nameof(CarStats)}] Min jump force must be less than max jump force on {name}!", this);
            }
            
            if (minMaxHealth >= maxMaxHealth)
            {
                Debug.LogWarning($"[{nameof(CarStats)}] Min max health must be less than max max health on {name}!", this);
            }
            
            if (minTurnSpeed >= maxTurnSpeed)
            {
                Debug.LogWarning($"[{nameof(CarStats)}] Min turn speed must be less than max turn speed on {name}!", this);
            }
        }
    }
}