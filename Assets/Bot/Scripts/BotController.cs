using Player.Scripts;
using UnityEngine;
using UnityEngine.AI;

namespace Bot.Scripts
{
    public class BotController : MonoBehaviour
    {
        [SerializeField] private CarStats carStats;
        
        public BotStates currentState = BotStates.Patrol;

        private Rigidbody rb;
        private NavMeshPath path;
        private Transform target;

        [Header("AI Settings")]
        [SerializeField] private float detectionRange = 25f;
        [SerializeField] private float attackRange = 10f;
        [SerializeField] private float patrolRadius = 20f;
        [SerializeField] private float patrolWaitTime = 2f;
        [SerializeField] private float pathUpdateRate = 1f;
        [SerializeField] private float waypointThreshold = 3f;

        private float acceleration;
        private float turnSpeed;


        private float patrolTimer;
        private int currentCorner = 0;
        private float nextPathUpdate;

        void Start()
        {
            rb = GetComponent<Rigidbody>();
            path = new NavMeshPath();
            SetRandomPatrolPoint();
            acceleration = carStats.acceleration;
            turnSpeed = carStats.turnSpeed;
        }

        void Update()
        {
            UpdateTarget();
            HandleStateTransitions();
        }

        void FixedUpdate()
        {
            switch (currentState)
            {
                case BotStates.Patrol:
                    Patrol();
                    break;
                case BotStates.Chase:
                    ChaseTarget();
                    break;
                case BotStates.Attack:
                    AttackTarget();
                    break;
            }
        }

        // ---------- STATE BEHAVIOR ----------

        private void Patrol()
        {
            if (Time.time > nextPathUpdate)
            {
                nextPathUpdate = Time.time + pathUpdateRate;
                if (path.corners.Length == 0 || currentCorner >= path.corners.Length)
                    SetRandomPatrolPoint();
            }

            DriveAlongPath();
        }

        private void ChaseTarget()
        {
            if (target == null) return;

            if (Time.time > nextPathUpdate)
            {
                nextPathUpdate = Time.time + pathUpdateRate;
                NavMesh.CalculatePath(transform.position, target.position, NavMesh.AllAreas, path);
                currentCorner = 0;
            }

            DriveAlongPath();
        }

        private void AttackTarget()
        {
            if (target == null) return;

            // Face target, stop moving, shoot or ram
            Vector3 dir = (target.position - transform.position).normalized;
            Quaternion lookRot = Quaternion.LookRotation(dir);
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, lookRot, 5f * Time.deltaTime));

            Debug.DrawRay(transform.position + Vector3.up, dir * 10f, Color.red);
            // TODO: Fire projectile logic here
        }

        // ---------- MOVEMENT / PATHING ----------

        private void DriveAlongPath()
        {
            if (path.corners.Length == 0) return;

            Vector3 corner = path.corners[currentCorner];
            Vector3 toCorner = corner - transform.position;
            toCorner.y = 0f;

            // If close to this corner, move to next one
            if (toCorner.magnitude < waypointThreshold && currentCorner < path.corners.Length - 1)
            {
                currentCorner++;
                return;
            }

            Vector3 dir = toCorner.normalized;
            float angle = Vector3.SignedAngle(transform.forward, dir, Vector3.up);
            float steering = Mathf.Clamp(angle / 45f, -1f, 1f);

            // Turn and move forward
            rb.AddTorque(Vector3.up * steering * turnSpeed);
            rb.AddRelativeForce(Vector3.forward * acceleration);
        }

        private void SetRandomPatrolPoint()
        {
            Vector3 randomDirection = Random.insideUnitSphere * patrolRadius + transform.position;
            if (NavMesh.SamplePosition(randomDirection, out NavMeshHit hit, patrolRadius, NavMesh.AllAreas))
            {
                NavMesh.CalculatePath(transform.position, hit.position, NavMesh.AllAreas, path);
                currentCorner = 0;
            }
        }

        // ---------- TARGETING / STATES ----------

        private void UpdateTarget()
        {
            var allCars = FindObjectsByType<CarHealth>(FindObjectsSortMode.None);
            var myTransform = transform;

            float minDist = Mathf.Infinity;
            Transform nearest = null;

            foreach (var car in allCars)
            {
                if (car.transform == myTransform) continue;

                float dist = Vector3.Distance(myTransform.position, car.transform.position);
                if (dist < minDist && dist <= detectionRange)
                {
                    minDist = dist;
                    nearest = car.transform;
                }
            }

            target = nearest;
        }

        private void HandleStateTransitions()
        {
            if (target == null)
            {
                currentState = BotStates.Patrol;
                return;
            }

            float distance = Vector3.Distance(transform.position, target.position);

            switch (currentState)
            {
                case BotStates.Patrol:
                    if (distance <= detectionRange)
                        currentState = BotStates.Chase;
                    break;

                case BotStates.Chase:
                    if (distance <= attackRange)
                        currentState = BotStates.Attack;
                    else if (distance > detectionRange)
                        currentState = BotStates.Patrol;
                    break;

                case BotStates.Attack:
                    if (distance > attackRange)
                        currentState = BotStates.Chase;
                    break;
            }
        }
    }
}
