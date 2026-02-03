using _Cars.ScriptableObjects;
using _Cars.Scripts;
using _Projectiles.ScriptableObjects;
using _Projectiles.Scripts;
using Bot.Scripts;
using UnityEngine;

namespace _Bot.Scripts
{
    public class BotAI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private AICarStats carStats;
        [SerializeField] private ProjectileObject projectile;
        
        private BotController botController;
        private Rigidbody botRigidbody;
        private Transform firePoint;
        
        private BotStates currentState = BotStates.Chase;
        private Transform target;
        private Transform lastAttacker;
        
        private float nextFireTime = 0f;
        private float targetCheckTimer = 0f;
        private float runAwayTimer = 0f;
        
        private const float TARGET_CHECK_INTERVAL = 2f;
        private const float RUN_AWAY_DURATION = 3f;
        private const float RAYCAST_ORIGIN_HEIGHT = 0.5f;
        private const float TURN_ANGLE_DIVISOR = 45f;
        private const float STUCK_CHECK_INTERVAL = 2f;
        private const float STUCK_MOVEMENT_THRESHOLD = 1f;
        private const float REVERSE_DURATION = 1f;
        
        private Vector3 lastPosition;
        private float stuckCheckTimer = 0f;
        private float reverseTimer = 0f;
        private bool isReversing = false;
        
        private void Awake()
        {
            InitializeComponents();
        }
        
        private void Start()
        {
            lastPosition = transform.position;
        }
        
        private void InitializeComponents()
        {
            botController = GetComponent<BotController>();
            botRigidbody = GetComponent<Rigidbody>();
            firePoint = transform.Find("FirePoint");
            
            ValidateComponents();
        }
        
        private void ValidateComponents()
        {
            if (botController == null)
            {
                Debug.LogError($"[{nameof(BotAI)}] BotController not found on {gameObject.name}!");
            }
            
            if (botRigidbody == null)
            {
                Debug.LogError($"[{nameof(BotAI)}] Rigidbody not found on {gameObject.name}!");
            }
            
            if (firePoint == null)
            {
                Debug.LogError($"[{nameof(BotAI)}] FirePoint not found on {gameObject.name}!");
            }
            
            if (carStats == null)
            {
                Debug.LogError($"[{nameof(BotAI)}] AICarStats not assigned on {gameObject.name}!");
            }
            
            if (projectile == null)
            {
                Debug.LogError($"[{nameof(BotAI)}] ProjectileObject not assigned on {gameObject.name}!");
            }
        }
        
        private void Update()
        {
            UpdateTargetSearch();
            CheckIfStuck();
            
            if (target == null)
            {
                return;
            }
            
            ExecuteCurrentState();
            HandleStateTransitions();
        }
        
        private void FixedUpdate()
        {
            HandleShooting();
        }
        
        private void UpdateTargetSearch()
        {
            targetCheckTimer += Time.deltaTime;
            
            if (ShouldFindNewTarget())
            {
                FindClosestTarget();
                targetCheckTimer = 0f;
            }
        }
        
        private bool ShouldFindNewTarget()
        {
            return targetCheckTimer >= TARGET_CHECK_INTERVAL;
        }
        
        private void CheckIfStuck()
        {
            stuckCheckTimer += Time.deltaTime;
            
            if (stuckCheckTimer >= STUCK_CHECK_INTERVAL)
            {
                if (IsStuck() && !isReversing)
                {
                    StartUnstuckRoutine();
                }
                
                lastPosition = transform.position;
                stuckCheckTimer = 0f;
            }
            
            if (isReversing)
            {
                HandleReverseTimer();
            }
        }
        
        private bool IsStuck()
        {
            float distanceMoved = Vector3.Distance(transform.position, lastPosition);
            return distanceMoved < STUCK_MOVEMENT_THRESHOLD && botController.GetSpeed() < 1f;
        }
        
        private void StartUnstuckRoutine()
        {
            isReversing = true;
            reverseTimer = 0f;
        }
        
        private void HandleReverseTimer()
        {
            reverseTimer += Time.deltaTime;
            
            if (reverseTimer >= REVERSE_DURATION)
            {
                isReversing = false;
                reverseTimer = 0f;
            }
        }
        
        private void ExecuteCurrentState()
        {
            if (isReversing)
            {
                ExecuteUnstuckRoutine();
                return;
            }
            
            switch (currentState)
            {
                case BotStates.Chase:
                case BotStates.Attack:
                    Chase();
                    break;
                case BotStates.RunAway:
                    RunAway();
                    break;
            }
        }
        
        private void ExecuteUnstuckRoutine()
        {
            float randomTurn = Random.value > 0.5f ? 1f : -1f;
            botController.SetInputs(randomTurn, -1f);
        }
        
        private void HandleStateTransitions()
        {
            if (currentState == BotStates.RunAway && RunAwayTimerEnded())
            {
                TransitionToChase();
            }
            
            switch (currentState)
            {
                case BotStates.Chase:
                    if (ReachedTarget())
                    {
                        SetState(BotStates.Attack);
                    }
                    break;
                    
                case BotStates.Attack:
                    if (!ReachedTarget())
                    {
                        SetState(BotStates.Chase);
                    }
                    break;
            }
        }
        
        private bool RunAwayTimerEnded()
        {
            return runAwayTimer >= RUN_AWAY_DURATION;
        }
        
        private void TransitionToChase()
        {
            runAwayTimer = 0f;
            SetState(BotStates.Chase);
        }
        
        private bool ReachedTarget()
        {
            return GetDistanceFromTarget() <= carStats.ReachedTargetDistance;
        }
        
        private void Chase()
        {
            ObstacleAvoidanceResult avoidance = HandleObstacleDetection();
            
            if (avoidance.isAvoiding)
            {
                botController.SetInputs(avoidance.turnInput, avoidance.moveInput);
            }
            else
            {
                NavigateToTarget();
            }
        }
        
        private void NavigateToTarget()
        {
            float turnInput = CalculateTurnInput();
            float moveInput = CalculateMoveInput();
            
            botController.SetInputs(turnInput, moveInput);
        }
        
        private float CalculateTurnInput()
        {
            if (ReachedTarget())
            {
                return 0f;
            }
            
            return IsTargetToTheRight() ? 1f : -1f;
        }
        
        private float CalculateMoveInput()
        {
            if (ReachedTarget())
            {
                return 0f;
            }
            
            if (!IsTargetInFront())
            {
                return 0f;
            }
            
            if (ShouldBrake())
            {
                return -1f;
            }
            
            return 1f;
        }
        
        private bool ShouldBrake()
        {
            return GetDistanceFromTarget() < carStats.StoppingDistance && 
                   botController.GetSpeed() > carStats.StoppingSpeed;
        }
        
        private struct ObstacleAvoidanceResult
        {
            public bool isAvoiding;
            public float turnInput;
            public float moveInput;
        }
        
        private ObstacleAvoidanceResult HandleObstacleDetection()
        {
            Vector3 origin = transform.position + Vector3.up * RAYCAST_ORIGIN_HEIGHT;
            Vector3 direction = transform.forward;
            
            if (!Physics.Raycast(origin, direction, out RaycastHit hit, carStats.ObstacleCheckDistance, carStats.ObstacleMask))
            {
                Debug.DrawRay(origin, direction * carStats.ObstacleCheckDistance, Color.blue);
                return new ObstacleAvoidanceResult { isAvoiding = false };
            }
            
            Debug.DrawRay(origin, direction * carStats.ObstacleCheckDistance, Color.red);
            
            float obstacleHeight = hit.point.y - transform.position.y;
            
            if (CanJumpOver(obstacleHeight))
            {
                botController.Jump();
                return new ObstacleAvoidanceResult { isAvoiding = false };
            }
            
            return NavigateAroundObstacle(origin);
        }
        
        private bool CanJumpOver(float obstacleHeight)
        {
            return obstacleHeight <= carStats.JumpableHeight && botController.IsGrounded();
        }
        
        private ObstacleAvoidanceResult NavigateAroundObstacle(Vector3 origin)
        {
            bool leftClear = !Physics.Raycast(origin, -transform.right, carStats.SideCheckDistance, carStats.ObstacleMask);
            bool rightClear = !Physics.Raycast(origin, transform.right, carStats.SideCheckDistance, carStats.ObstacleMask);
            
            Debug.DrawRay(origin, -transform.right * carStats.SideCheckDistance, leftClear ? Color.green : Color.red);
            Debug.DrawRay(origin, transform.right * carStats.SideCheckDistance, rightClear ? Color.green : Color.red);
            
            float turnInput = DetermineTurnDirection(leftClear, rightClear);
            float moveInput = DetermineAvoidanceSpeed(leftClear, rightClear);
            
            return new ObstacleAvoidanceResult
            {
                isAvoiding = true,
                turnInput = turnInput,
                moveInput = moveInput
            };
        }
        
        private float DetermineTurnDirection(bool leftClear, bool rightClear)
        {
            if (leftClear && !rightClear)
            {
                return -carStats.AvoidanceTurnStrength;
            }
            
            if (!leftClear && rightClear)
            {
                return carStats.AvoidanceTurnStrength;
            }
            
            return Random.value > 0.5f ? carStats.AvoidanceTurnStrength : -carStats.AvoidanceTurnStrength;
        }
        
        private float DetermineAvoidanceSpeed(bool leftClear, bool rightClear)
        {
            if (leftClear || rightClear)
            {
                return 0.5f;
            }
            
            return -0.3f;
        }
        
        private void RunAway()
        {
            if (lastAttacker == null)
            {
                TransitionToChase();
                return;
            }
            
            ObstacleAvoidanceResult avoidance = HandleObstacleDetection();
            
            if (avoidance.isAvoiding)
            {
                botController.SetInputs(avoidance.turnInput, avoidance.moveInput);
            }
            else
            {
                NavigateAwayFromAttacker();
            }
            
            UpdateRunAwayTimer();
        }
        
        private void NavigateAwayFromAttacker()
        {
            Vector3 awayDirection = (transform.position - lastAttacker.position).normalized;
            float turnAmount = CalculateRunAwayTurnAmount(awayDirection);
            
            botController.SetInputs(turnAmount, 1f);
        }
        
        private float CalculateRunAwayTurnAmount(Vector3 awayDirection)
        {
            float angle = Vector3.SignedAngle(transform.forward, awayDirection, Vector3.up);
            return Mathf.Clamp(angle / TURN_ANGLE_DIVISOR, -1f, 1f);
        }
        
        private void UpdateRunAwayTimer()
        {
            runAwayTimer += Time.deltaTime;
            
            if (runAwayTimer >= RUN_AWAY_DURATION)
            {
                TransitionToChase();
            }
        }
        
        private void HandleShooting()
        {
            if (currentState == BotStates.Attack && CanShoot())
            {
                Shoot();
                nextFireTime = Time.time + projectile.FireRate;
            }
        }
        
        private bool CanShoot()
        {
            return Time.time > nextFireTime;
        }
        
        private void Shoot()
        {
            Vector3 shootDirection = GetShootDirection();
            
            if (shootDirection == Vector3.zero)
            {
                return;
            }
            
            GameObject bullet = InstantiateBullet(shootDirection);
            ConfigureBullet(bullet, shootDirection);
        }
        
        private Vector3 GetShootDirection()
        {
            if (target == null || firePoint == null)
            {
                return Vector3.zero;
            }
            
            return (target.position - firePoint.position).normalized;
        }
        
        private GameObject InstantiateBullet(Vector3 shootDirection)
        {
            return Instantiate(
                projectile.ProjectilePrefab, 
                firePoint.position, 
                Quaternion.LookRotation(shootDirection)
            );
        }
        
        private void ConfigureBullet(GameObject bullet, Vector3 shootDirection)
        {
            Projectile proj = bullet.GetComponent<Projectile>();
            if (proj != null)
            {
                proj.SetShooter(gameObject);
            }
            
            Rigidbody bulletRb = bullet.GetComponent<Rigidbody>();
            if (bulletRb != null && botRigidbody != null)
            {
                bulletRb.linearVelocity = botRigidbody.linearVelocity;
                bulletRb.AddForce(shootDirection * projectile.FireForce, ForceMode.Impulse);
            }
        }
        
        private bool IsTargetToTheRight()
        {
            return GetTurnAngle() > 0;
        }
        
        private float GetTurnAngle()
        {
            return Vector3.SignedAngle(transform.forward, GetTargetDirection(), Vector3.up);
        }
        
        private bool IsTargetInFront()
        {
            return Vector3.Dot(transform.forward, GetTargetDirection()) > 0;
        }
        
        private Vector3 GetTargetDirection()
        {
            if (target == null)
            {
                return transform.forward;
            }
            
            return (target.position - transform.position).normalized;
        }
        
        private float GetDistanceFromTarget()
        {
            if (target == null)
            {
                return Mathf.Infinity;
            }
            
            return Vector3.Distance(target.position, transform.position);
        }
        
        private void FindClosestTarget()
        {
            float closestDistance = Mathf.Infinity;
            Transform closestTarget = null;
            
            CarHealth[] allCars = FindObjectsByType<CarHealth>(FindObjectsSortMode.None);
            
            foreach (CarHealth car in allCars)
            {
                if (car.gameObject == gameObject)
                {
                    continue;
                }
                
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
        
        private void SetState(BotStates newState)
        {
            currentState = newState;
        }
        
        public void OnHit(Transform attacker)
        {
            lastAttacker = attacker;
            runAwayTimer = 0f;
            SetState(BotStates.RunAway);
        }
    }
}