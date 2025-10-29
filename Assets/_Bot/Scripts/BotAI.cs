using _Cars.ScriptableObjects;
using _Cars.Scripts;
using _Projectiles.ScriptableObjects;
using _Projectiles.Scripts;
using Bot.Scripts;
using UnityEngine;
using Quaternion = UnityEngine.Quaternion;
using Vector3 = UnityEngine.Vector3;

namespace _Bot.Scripts
{
    public class BotAI : MonoBehaviour
    {
                
        [SerializeField] private AICarStats carStats;
        [SerializeField] private ProjectileObject projectile;
        private BotController botController;
        
        public BotStates currentState = BotStates.Chase;
        
        public Transform target;
        
        // Run Away
        private Transform lastAttacker;
        private float runAwayDuration = 3f;
        private float runAwayTimer;

        // Shooting
        private Transform firePoint;
        private bool isFiring = false;
        private float nextFireTime = 0f;
        
        // Target
        private float targetCheckInterval = 2f;
        private float targetCheckTimer = 0f;

        void Awake()
        {
            botController = GetComponent<BotController>();
            firePoint = transform.Find("FirePoint");
        }

        void Update()
        {
            targetCheckTimer += Time.deltaTime;
            if (targetCheckTimer >= targetCheckInterval)
            {
                FindClosestTarget();
                targetCheckTimer = 0f;
            }

            if (target == null) return;
            
            switch (currentState)
            {
                case BotStates.Chase:
                    Chase();
                    break;
                case BotStates.Attack:
                    Chase();
                    break;
                case BotStates.RunAway:
                    RunAway();
                    break;
            }

            HandleStateTransitions();
        }

        void FixedUpdate()
        {
            if (currentState == BotStates.Attack && Time.time > nextFireTime)
            {
                Shoot();
                nextFireTime = Time.time + projectile.fireRate;
            }
        }

        private void HandleStateTransitions()
        {
            // if (runAwayTimer >= runAwayDuration)
            //     SetState(BotStates.Chase);

            switch (currentState) {
                case BotStates.Chase:
                    if (GetDistanceFromTarget() <= carStats.reachedTargetDistance)
                    {
                        SetState(BotStates.Attack);
                    }
                    break;
                case BotStates.Attack:
                    if (GetDistanceFromTarget() > carStats.reachedTargetDistance)
                    {
                        SetState(BotStates.Chase);
                    }

                    break;
                case BotStates.RunAway:
                    break;
        }
    }

        private void Chase()
        {
            bool avoiding = HandleObstacleDetection();

            if (!avoiding)
            {
                float turnAmount = 0f;
                float moveAmount = 0f;
            
                var distanceFromTarget = GetDistanceFromTarget();

                if (distanceFromTarget > carStats.reachedTargetDistance)
                {
                    Vector3 direction = (target.position - transform.position).normalized;
                    float dotProduct = Vector3.Dot(transform.forward, direction);

                    if (dotProduct > 0)
                    {
                        moveAmount = 1f;
                    
                        if (distanceFromTarget < carStats.stoppingDistance && botController.GetSpeed() > carStats.stoppingSpeed)
                        {
                            moveAmount = -1f;
                        }
                    }
                
                    float turnAngle = Vector3.SignedAngle(transform.forward, direction, Vector3.up);

                    if (turnAngle > 0)
                    {
                        turnAmount = 1f;
                    }
                    else
                    {
                        turnAmount = -1f;
                    }
                }
                else
                {
                    turnAmount = 0f;
                    moveAmount = 0f;
                }
            
                botController.SetInputs(turnAmount, moveAmount);
            }
        }

        private float GetDistanceFromTarget()
        {
            float distanceFromTarget = Vector3.Distance(target.position, transform.position);
            return distanceFromTarget;
        }

        private bool HandleObstacleDetection()
        {
            RaycastHit hit;
            Vector3 origin = transform.position + Vector3.up * 0.5f;
            Vector3 direction = transform.forward;

            bool hitSomething = Physics.Raycast(origin, direction, out hit, carStats.obstacleCheckDistance, carStats.obstacleMask);

            if (hitSomething)
            {
                float obstacleHeight = hit.point.y - transform.position.y;

                // Draw ray for debugging
                Debug.DrawRay(origin, direction * carStats.obstacleCheckDistance, Color.red);

                // Jump if low obstacle
                if (obstacleHeight <= carStats.jumpableHeight && botController.IsGrounded())
                {
                    botController.Jump();
                    return false; // we still move forward
                }

                // Too tall — steer around
                else
                {
                    // Check left and right rays
                    bool leftClear = !Physics.Raycast(origin, -transform.right, carStats.sideCheckDistance, carStats.obstacleMask);
                    bool rightClear = !Physics.Raycast(origin, transform.right, carStats.sideCheckDistance, carStats.obstacleMask);

                    float turn = 0f;
                    if (leftClear && !rightClear)
                        turn = -carStats.avoidanceTurnStrength; // steer left
                    else if (!leftClear && rightClear)
                        turn = carStats.avoidanceTurnStrength; // steer right
                    else
                        turn = (Random.value > 0.5f) ? carStats.avoidanceTurnStrength : -carStats.avoidanceTurnStrength;

                    // Back off slightly to avoid getting stuck
                    botController.SetInputs(turn, -0.5f);

                    Debug.DrawRay(origin, transform.right * carStats.sideCheckDistance, Color.green);
                    Debug.DrawRay(origin, -transform.right * carStats.sideCheckDistance, Color.green);

                    return true; // we are currently avoiding
                }
            }
            else
            {
                // No obstacle detected
                Debug.DrawRay(origin, direction * carStats.obstacleCheckDistance, Color.blue);
                return false;
            }
        }

        private void Shoot()
        {
            Vector3 shootDirection = GetDirection();

            GameObject bullet = Instantiate(projectile.projectilePrefab, firePoint.position, Quaternion.LookRotation(shootDirection));

            Projectile proj = bullet.GetComponent<Projectile>();
            if (proj != null)
            {
                proj.SetShooter(gameObject); // "gameObject" = the car that fired
            }
            
            Rigidbody bulletRb = bullet.GetComponent<Rigidbody>();
            if (bulletRb != null)
            {
                bulletRb.linearVelocity = GetComponent<Rigidbody>().linearVelocity; // inherit car’s current movement
                bulletRb.AddForce(shootDirection * projectile.fireForce, ForceMode.Impulse); // add firing force
            }

            bullet.GetComponent<Rigidbody>().AddForce(shootDirection * projectile.fireForce, ForceMode.Impulse);
        }

        private Vector3 GetDirection()
        {
            if (target != null)
            {
                Vector3 shootDirection = (target.position - firePoint.position).normalized;
                return shootDirection;  
            }

            return Vector3.zero; ;

        }

        private void SetTargetPosition(Vector3 targetPosition)
        {
            target.position = targetPosition;
        }

        private void SetState(BotStates state)
        {
            currentState = state;
        }
        
        public void OnHit(Transform attacker)
        {
            lastAttacker = attacker;
            SetState(BotStates.RunAway);
        }

        
        private void RunAway()
        {
            if (lastAttacker == null)
            {
                SetState(BotStates.Chase);
                return;
            }

            // calculate opposite direction
            Vector3 awayDir = (transform.position - lastAttacker.position).normalized;
            float turnAmount = Mathf.Clamp(Vector3.SignedAngle(transform.forward, awayDir, Vector3.up) / 45f, -1f, 1f);

            botController.SetInputs(turnAmount, 1f); // drive away

            // timer to exit state
            runAwayTimer += Time.deltaTime;
            if (runAwayTimer >= runAwayDuration)
            {
                runAwayTimer = 0f;
                SetState(BotStates.Chase);
            }
        }

        private void FindClosestTarget()
        {
            float closestDistance = Mathf.Infinity;
            Transform closestTarget = null;

            // Find all cars that have CarHealth (so they can be shot)
            var allCars = FindObjectsByType<CarHealth>(FindObjectsSortMode.None);

            foreach (var car in allCars)
            {
                // Skip yourself
                if (car.gameObject == this.gameObject) continue;

                float distance = Vector3.Distance(transform.position, car.transform.position);

                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestTarget = car.transform;
                }
            }

            if (closestTarget != null)
            {
                target = closestTarget;
            }
        }
        
        
        
    }
        
}