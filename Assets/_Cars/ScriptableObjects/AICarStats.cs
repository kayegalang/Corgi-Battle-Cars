using UnityEngine;

namespace _Cars.ScriptableObjects
{
    [CreateAssetMenu(fileName = "AICarStats", menuName = "Scriptable Objects/AICarStats")]
    public class AICarStats : ScriptableObject
    {
        [Header("Movement Settings")]
        public float reachedTargetDistance;
        public float reverseDistance;
        public float stoppingDistance;
        public float stoppingSpeed;
        
        [Header("Obstacle Avoidance Settings")]
        public float obstacleCheckDistance;   
        public float sideCheckDistance;       
        public float jumpableHeight;          
        public LayerMask obstacleMask;             
        public float avoidanceTurnStrength;
    }
}

