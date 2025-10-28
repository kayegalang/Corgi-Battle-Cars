using UnityEngine;
using UnityEngine.AI;

namespace Bot.Scripts
{
    public class BotPath : MonoBehaviour
    {
        [Header("Wandering Settings")]
        [SerializeField] private float wanderRadius = 120f;     
        [SerializeField] private float recalcDistance = 5f;     
        [SerializeField] private float recalcDelay = 2f;        

        private Vector3 currentTarget;
        private float recalcTimer;

        private void Start()
        {
            PickNewTarget();
        }

        private void Update()
        {
            recalcTimer += Time.deltaTime;

            float dist = Vector3.Distance(transform.position, currentTarget);

            // Check if we should pick a new destination
            if (dist < recalcDistance || recalcTimer >= recalcDelay)
            {
                PickNewTarget();
                recalcTimer = 0f;
            }

            // Draw gizmos for debugging
            Debug.DrawLine(transform.position, currentTarget, Color.cyan);
            Debug.DrawRay(currentTarget, Vector3.up * 2f, Color.green);
        }

        private void PickNewTarget()
        {
            // Pick a random point on the NavMesh
            Vector3 randomDirection = Random.insideUnitSphere * wanderRadius;
            randomDirection += transform.position;

            if (NavMesh.SamplePosition(randomDirection, out NavMeshHit hit, wanderRadius, NavMesh.AllAreas))
            {
                currentTarget = hit.position;

                // Snap to ground level for physics-based cars
                if (Physics.Raycast(currentTarget + Vector3.up * 5f, Vector3.down, out RaycastHit groundHit, 10f))
                {
                    currentTarget = groundHit.point;
                }

                Debug.Log($"[BotNavMeshWanderer] New target: {currentTarget}");
            }
            else
            {
                Debug.LogWarning("[BotNavMeshWanderer] Failed to find valid NavMesh point.");
            }
        }

        public Vector3 GetTargetPoint()
        {
            return currentTarget;
        }
    }
}
