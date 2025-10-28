using System.Numerics;
using Bot.Scripts;
using Player.Scripts;
using UnityEditor.VersionControl;
using UnityEngine;
using Quaternion = UnityEngine.Quaternion;
using Vector3 = UnityEngine.Vector3;

namespace BotScript
{
    public class BotAI : MonoBehaviour
    {
        public BotStates currentState = BotStates.Chase;
        
        private BotController botController;
        
        public Transform target;
        
        [Header("Movement Settings")]
        [SerializeField] private float reachedTargetDistance = 10f;
        [SerializeField] private float reverseDistance = 25f;
        [SerializeField] private float stoppingDistance = 15f;
        [SerializeField] private float stoppingSpeed = 10f;
        
        [Header("Obstacle Avoidance Settings")]
        [SerializeField] private float obstacleCheckDistance = 10f;   
        [SerializeField] private float sideCheckDistance = 4f;       
        [SerializeField] private float jumpableHeight = 0.6f;          
        [SerializeField] private LayerMask obstacleMask;             
        [SerializeField] private float avoidanceTurnStrength = 1f;
        
        // Shooting
        public GameObject projectilePrefab;
        [SerializeField] private Transform firePoint;
        [SerializeField] private float fireForce = 30f;
        [SerializeField] private float fireRate = 0.25f;
        private bool isFiring = false;
        private float nextFireTime = 0f;
        
        // Run Away
        private Transform lastAttacker;
        private float runAwayDuration = 3f;
        private float runAwayTimer;


        void Awake()
        {
            botController = GetComponent<BotController>();
            target = GameObject.FindGameObjectWithTag("PlayerOne").transform;
        }

        void Update()
        {
            SetTargetPosition(target.position);
            
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
                nextFireTime = Time.time + fireRate;
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

                if (distanceFromTarget > reachedTargetDistance)
                {
                    Vector3 direction = (target.position - transform.position).normalized;
                    float dotProduct = Vector3.Dot(transform.forward, direction);

                    if (dotProduct > 0)
                    {
                        moveAmount = 1f;
                    
                        if (distanceFromTarget < stoppingDistance && botController.GetSpeed() > stoppingSpeed)
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

        private void HandleStateTransitions()
        {
            if (currentState == BotStates.Chase && GetDistanceFromTarget() <= reachedTargetDistance)
            {
                currentState = BotStates.Attack;
            }

            if (currentState == BotStates.Attack && GetDistanceFromTarget() > reachedTargetDistance)
            {
                currentState = BotStates.Chase;
            }
            
            if (runAwayTimer >= runAwayDuration)
                SetState(BotStates.Chase);
        }

        private bool HandleObstacleDetection()
        {
            RaycastHit hit;
            Vector3 origin = transform.position + Vector3.up * 0.5f;
            Vector3 direction = transform.forward;

            bool hitSomething = Physics.Raycast(origin, direction, out hit, obstacleCheckDistance, obstacleMask);

            if (hitSomething)
            {
                float obstacleHeight = hit.point.y - transform.position.y;

                // Draw ray for debugging
                Debug.DrawRay(origin, direction * obstacleCheckDistance, Color.red);

                // Jump if low obstacle
                if (obstacleHeight <= jumpableHeight && botController.IsGrounded())
                {
                    botController.Jump();
                    return false; // we still move forward
                }

                // Too tall — steer around
                else
                {
                    // Check left and right rays
                    bool leftClear = !Physics.Raycast(origin, -transform.right, sideCheckDistance, obstacleMask);
                    bool rightClear = !Physics.Raycast(origin, transform.right, sideCheckDistance, obstacleMask);

                    float turn = 0f;
                    if (leftClear && !rightClear)
                        turn = -avoidanceTurnStrength; // steer left
                    else if (!leftClear && rightClear)
                        turn = avoidanceTurnStrength; // steer right
                    else
                        turn = (Random.value > 0.5f) ? avoidanceTurnStrength : -avoidanceTurnStrength;

                    // Back off slightly to avoid getting stuck
                    botController.SetInputs(turn, -0.5f);

                    Debug.DrawRay(origin, transform.right * sideCheckDistance, Color.green);
                    Debug.DrawRay(origin, -transform.right * sideCheckDistance, Color.green);

                    return true; // we are currently avoiding
                }
            }
            else
            {
                // No obstacle detected
                Debug.DrawRay(origin, direction * obstacleCheckDistance, Color.blue);
                return false;
            }
        }

        private void Shoot()
        {
            Vector3 shootDirection = GetDirection();

            GameObject bullet = Instantiate(projectilePrefab, firePoint.position, Quaternion.LookRotation(shootDirection));

            Projectile proj = bullet.GetComponent<Projectile>();
            if (proj != null)
            {
                proj.SetShooter(gameObject); // "gameObject" = the car that fired
            }
            
            Rigidbody bulletRb = bullet.GetComponent<Rigidbody>();
            if (bulletRb != null)
            {
                bulletRb.linearVelocity = GetComponent<Rigidbody>().linearVelocity; // inherit car’s current movement
                bulletRb.AddForce(shootDirection * fireForce, ForceMode.Impulse); // add firing force
            }

            bullet.GetComponent<Rigidbody>().AddForce(shootDirection * fireForce, ForceMode.Impulse);
        }

        private Vector3 GetDirection()
        {
            Vector3 shootDirection = (target.position - firePoint.position).normalized;
            return shootDirection;
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



    }
        
}