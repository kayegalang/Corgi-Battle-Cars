using UnityEngine;

namespace _Cars.ScriptableObjects
{
    [CreateAssetMenu(fileName = "AICarStats", menuName = "Scriptable Objects/AICarStats")]
    public class AICarStats : ScriptableObject
    {
        [Header("Movement Settings")]
        [Tooltip("Distance at which the AI considers it has reached its target")]
        [SerializeField] [Range(0.5f, 10f)] private float reachedTargetDistance = 3f;
        
        [Tooltip("Distance at which the AI will reverse to avoid getting stuck")]
        [SerializeField] [Range(0.5f, 10f)] private float reverseDistance = 2f;
        
        [Tooltip("Distance at which the AI starts slowing down")]
        [SerializeField] [Range(1f, 20f)] private float stoppingDistance = 5f;
        
        [Tooltip("Speed threshold for when to apply brakes")]
        [SerializeField] [Range(1f, 20f)] private float stoppingSpeed = 10f;
        
        [Header("Obstacle Avoidance Settings")]
        [Tooltip("How far ahead to check for obstacles")]
        [SerializeField] [Range(1f, 20f)] private float obstacleCheckDistance = 5f;
        
        [Tooltip("How far to the sides to check for clear paths")]
        [SerializeField] [Range(1f, 10f)] private float sideCheckDistance = 3f;
        
        [Tooltip("Maximum height obstacle the AI can jump over")]
        [SerializeField] [Range(0.1f, 3f)] private float jumpableHeight = 0.5f;
        
        [Tooltip("Layer mask for detecting obstacles")]
        [SerializeField] private LayerMask obstacleMask;
        
        [Tooltip("How strongly the AI turns to avoid obstacles")]
        [SerializeField] [Range(0.1f, 2f)] private float avoidanceTurnStrength = 1f;
        
        public float ReachedTargetDistance => reachedTargetDistance;
        public float ReverseDistance => reverseDistance;
        public float StoppingDistance => stoppingDistance;
        public float StoppingSpeed => stoppingSpeed;
        public float ObstacleCheckDistance => obstacleCheckDistance;
        public float SideCheckDistance => sideCheckDistance;
        public float JumpableHeight => jumpableHeight;
        public LayerMask ObstacleMask => obstacleMask;
        public float AvoidanceTurnStrength => avoidanceTurnStrength;
        
        private void OnValidate()
        {
            ValidateReachedTargetDistance();
            ValidateReverseDistance();
            ValidateStoppingDistance();
            ValidateStoppingSpeed();
            ValidateObstacleCheckDistance();
            ValidateSideCheckDistance();
            ValidateJumpableHeight();
            ValidateAvoidanceTurnStrength();
        }
        
        private void ValidateReachedTargetDistance()
        {
            if (reachedTargetDistance <= 0f)
            {
                Debug.LogWarning($"[{nameof(AICarStats)}] Reached target distance should be greater than 0 on {name}!", this);
                reachedTargetDistance = 3f;
            }
        }
        
        private void ValidateReverseDistance()
        {
            if (reverseDistance <= 0f)
            {
                Debug.LogWarning($"[{nameof(AICarStats)}] Reverse distance should be greater than 0 on {name}!", this);
                reverseDistance = 2f;
            }
        }
        
        private void ValidateStoppingDistance()
        {
            if (stoppingDistance <= 0f)
            {
                Debug.LogWarning($"[{nameof(AICarStats)}] Stopping distance should be greater than 0 on {name}!", this);
                stoppingDistance = 5f;
            }
        }
        
        private void ValidateStoppingSpeed()
        {
            if (stoppingSpeed <= 0f)
            {
                Debug.LogWarning($"[{nameof(AICarStats)}] Stopping speed should be greater than 0 on {name}!", this);
                stoppingSpeed = 10f;
            }
        }
        
        private void ValidateObstacleCheckDistance()
        {
            if (obstacleCheckDistance <= 0f)
            {
                Debug.LogWarning($"[{nameof(AICarStats)}] Obstacle check distance should be greater than 0 on {name}!", this);
                obstacleCheckDistance = 5f;
            }
        }
        
        private void ValidateSideCheckDistance()
        {
            if (sideCheckDistance <= 0f)
            {
                Debug.LogWarning($"[{nameof(AICarStats)}] Side check distance should be greater than 0 on {name}!", this);
                sideCheckDistance = 3f;
            }
        }
        
        private void ValidateJumpableHeight()
        {
            if (jumpableHeight <= 0f)
            {
                Debug.LogWarning($"[{nameof(AICarStats)}] Jumpable height should be greater than 0 on {name}!", this);
                jumpableHeight = 0.5f;
            }
        }
        
        private void ValidateAvoidanceTurnStrength()
        {
            if (avoidanceTurnStrength <= 0f)
            {
                Debug.LogWarning($"[{nameof(AICarStats)}] Avoidance turn strength should be greater than 0 on {name}!", this);
                avoidanceTurnStrength = 1f;
            }
        }
    }
}