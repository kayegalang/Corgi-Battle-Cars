using _Cars.ScriptableObjects;
using _Cars.Scripts;
using _Projectiles.ScriptableObjects;
using _Projectiles.Scripts;
using _UI.Scripts;
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
        private Rigidbody     botRigidbody;
        private Transform     firePoint;
        private CooldownBarUI cooldownBar;
        
        private BotStates currentState  = BotStates.Chase;
        private Transform target;
        private Transform lastAttacker;
        
        private float nextFireTime     = 0f;
        private float targetCheckTimer = 0f;
        private float runAwayTimer     = 0f;
        
        private const float TARGET_CHECK_INTERVAL = 2f;
        private const float RUN_AWAY_DURATION     = 3f;
        private const float RAYCAST_ORIGIN_HEIGHT = 0.5f;
        private const float TURN_ANGLE_DIVISOR    = 45f;

        // Stuck detection
        private const float STUCK_CHECK_INTERVAL     = 0.5f;
        private const float STUCK_MOVEMENT_THRESHOLD = 0.5f;
        private const float STUCK_SPEED_THRESHOLD    = 0.8f;
        private const float REVERSE_DURATION         = 1.2f;
        private const float UNSTUCK_TURN_STRENGTH    = 0.8f;

        // Rear check distance when reversing
        private const float REAR_CHECK_DISTANCE = 2f;

        // Charge / overheat
        private float currentCharge;
        private const float MAX_CHARGE      = 100f;
        private const float CHARGE_PER_SHOT = 10f;
        private float chargeRegenRate;
        private float fireRate;
        private bool  isOverheated = false;
        
        private Vector3 lastPosition;
        private float   stuckCheckTimer = 0f;
        private float   reverseTimer    = 0f;
        private bool    isReversing     = false;
        private float   unstuckTurnDir  = 1f;
        private int     stuckCount      = 0;

        // ═══════════════════════════════════════════════
        //  LIFECYCLE
        // ═══════════════════════════════════════════════

        private void Awake()
        {
            InitializeComponents();
            InitializeFromProjectile();
            currentCharge = MAX_CHARGE;
        }
        
        private void Start()
        {
            lastPosition = transform.position;
            StartCoroutine(FindCooldownBarDelayed());
        }

        private System.Collections.IEnumerator FindCooldownBarDelayed()
        {
            yield return null;
            cooldownBar = GetComponentInChildren<CooldownBarUI>();
            if (cooldownBar == null)
                Debug.LogWarning($"[{nameof(BotAI)}] {gameObject.name} - No CooldownBarUI found.");
        }

        private void InitializeFromProjectile()
        {
            if (projectile == null) return;
            fireRate        = projectile.FireRate;
            chargeRegenRate = MAX_CHARGE / Mathf.Max(projectile.CooldownDuration, 0.1f);
        }
        
        private void InitializeComponents()
        {
            botController = GetComponent<BotController>();
            botRigidbody  = GetComponent<Rigidbody>();
            firePoint     = transform.Find("FirePoint");
            ValidateComponents();
        }
        
        private void ValidateComponents()
        {
            if (botController == null) Debug.LogError($"[{nameof(BotAI)}] BotController not found on {gameObject.name}!");
            if (botRigidbody  == null) Debug.LogError($"[{nameof(BotAI)}] Rigidbody not found on {gameObject.name}!");
            if (firePoint     == null) Debug.LogError($"[{nameof(BotAI)}] FirePoint not found on {gameObject.name}!");
            if (carStats      == null) Debug.LogError($"[{nameof(BotAI)}] AICarStats not assigned on {gameObject.name}!");
            if (projectile    == null) Debug.LogError($"[{nameof(BotAI)}] ProjectileObject not assigned on {gameObject.name}!");
        }

        // ═══════════════════════════════════════════════
        //  UPDATE
        // ═══════════════════════════════════════════════
        
        private void Update()
        {
            UpdateTargetSearch();
            CheckIfStuck();
            UpdateCooldownBar();
            
            if (target == null) return;
            
            ExecuteCurrentState();
            HandleStateTransitions();
        }
        
        private void FixedUpdate()
        {
            HandleShooting();
        }

        private void UpdateCooldownBar()
        {
            if (cooldownBar == null) return;
            cooldownBar.SetCooldown(currentCharge, MAX_CHARGE);
        }
        
        private void UpdateTargetSearch()
        {
            targetCheckTimer += Time.deltaTime;
            if (targetCheckTimer >= TARGET_CHECK_INTERVAL)
            {
                FindClosestTarget();
                targetCheckTimer = 0f;
            }
        }

        // ═══════════════════════════════════════════════
        //  STUCK DETECTION
        // ═══════════════════════════════════════════════
        
        private void CheckIfStuck()
        {
            stuckCheckTimer += Time.deltaTime;
            
            if (stuckCheckTimer >= STUCK_CHECK_INTERVAL)
            {
                if (!isReversing && IsStuck())
                {
                    stuckCount++;
                    StartUnstuckRoutine();
                }
                else if (!IsStuck())
                {
                    stuckCount = 0;
                }

                lastPosition    = transform.position;
                stuckCheckTimer = 0f;
            }
            
            if (isReversing)
                HandleReverseTimer();
        }
        
        private bool IsStuck()
        {
            float distanceMoved = Vector3.Distance(transform.position, lastPosition);
            float speed         = botController.GetSpeed();
            return distanceMoved < STUCK_MOVEMENT_THRESHOLD && speed < STUCK_SPEED_THRESHOLD;
        }
        
        private void StartUnstuckRoutine()
        {
            isReversing  = true;
            reverseTimer = 0f;

            Vector3 origin     = transform.position + Vector3.up * RAYCAST_ORIGIN_HEIGHT;
            bool    leftClear  = !Physics.Raycast(origin, -transform.right, carStats.SideCheckDistance, carStats.ObstacleMask);
            bool    rightClear = !Physics.Raycast(origin,  transform.right, carStats.SideCheckDistance, carStats.ObstacleMask);

            if      (leftClear  && !rightClear) unstuckTurnDir = -1f;
            else if (!leftClear && rightClear)  unstuckTurnDir =  1f;
            else                                unstuckTurnDir =  (stuckCount % 2 == 0) ? 1f : -1f;
        }
        
        private void HandleReverseTimer()
        {
            reverseTimer += Time.deltaTime;
            if (reverseTimer >= REVERSE_DURATION)
            {
                isReversing  = false;
                reverseTimer = 0f;
            }
        }

        // ═══════════════════════════════════════════════
        //  STATE EXECUTION
        // ═══════════════════════════════════════════════
        
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
            Vector3 origin      = transform.position + Vector3.up * RAYCAST_ORIGIN_HEIGHT;
            Vector3 rearCenter  = -transform.forward;
            Vector3 rearLeft    = (-transform.forward - transform.right * 0.5f).normalized;
            Vector3 rearRight   = (-transform.forward + transform.right * 0.5f).normalized;

            bool rearCenterBlocked = Physics.Raycast(origin, rearCenter,  REAR_CHECK_DISTANCE, carStats.ObstacleMask);
            bool rearLeftBlocked   = Physics.Raycast(origin, rearLeft,    REAR_CHECK_DISTANCE, carStats.ObstacleMask);
            bool rearRightBlocked  = Physics.Raycast(origin, rearRight,   REAR_CHECK_DISTANCE, carStats.ObstacleMask);

            Debug.DrawRay(origin, rearCenter * REAR_CHECK_DISTANCE, rearCenterBlocked ? Color.red : Color.cyan);
            Debug.DrawRay(origin, rearLeft   * REAR_CHECK_DISTANCE, rearLeftBlocked   ? Color.red : Color.cyan);
            Debug.DrawRay(origin, rearRight  * REAR_CHECK_DISTANCE, rearRightBlocked  ? Color.red : Color.cyan);

            bool canReverse = !rearCenterBlocked && !rearLeftBlocked && !rearRightBlocked;

            if (canReverse)
                botController.SetInputs(unstuckTurnDir * UNSTUCK_TURN_STRENGTH, -1f);
            else
                botController.SetInputs(unstuckTurnDir, 0f);
        }
        
        private void HandleStateTransitions()
        {
            if (currentState == BotStates.RunAway && runAwayTimer >= RUN_AWAY_DURATION)
                TransitionToChase();
            
            switch (currentState)
            {
                case BotStates.Chase:
                    if (ReachedTarget()) SetState(BotStates.Attack);
                    break;
                case BotStates.Attack:
                    if (!ReachedTarget()) SetState(BotStates.Chase);
                    break;
            }
        }
        
        private void TransitionToChase()
        {
            runAwayTimer = 0f;
            SetState(BotStates.Chase);
        }
        
        private bool ReachedTarget() => GetDistanceFromTarget() <= carStats.ReachedTargetDistance;

        // ═══════════════════════════════════════════════
        //  CHASE / RUN AWAY
        // ═══════════════════════════════════════════════
        
        private void Chase()
        {
            ObstacleAvoidanceResult avoidance = HandleObstacleDetection();
            
            if (avoidance.isAvoiding)
                botController.SetInputs(avoidance.turnInput, avoidance.moveInput);
            else
                NavigateToTarget();
        }
        
        private void NavigateToTarget()
        {
            float turnInput = CalculateTurnInput();
            float moveInput = CalculateMoveInput();
            botController.SetInputs(turnInput, moveInput);
        }
        
        private float CalculateTurnInput()
        {
            if (ReachedTarget()) return 0f;
            return IsTargetToTheRight() ? 1f : -1f;
        }
        
        private float CalculateMoveInput()
        {
            if (ReachedTarget())    return 0f;
            if (!IsTargetInFront()) return 0f;
            if (ShouldBrake())      return -1f;
            return 1f;
        }
        
        private bool ShouldBrake() =>
            GetDistanceFromTarget() < carStats.StoppingDistance &&
            botController.GetSpeed() > carStats.StoppingSpeed;

        // ═══════════════════════════════════════════════
        //  OBSTACLE DETECTION — front dual raycasts
        // ═══════════════════════════════════════════════
        
        private struct ObstacleAvoidanceResult
        {
            public bool  isAvoiding;
            public float turnInput;
            public float moveInput;
        }
        
        private ObstacleAvoidanceResult HandleObstacleDetection()
        {
            Vector3 centerOrigin = transform.position + Vector3.up * RAYCAST_ORIGIN_HEIGHT;
            Vector3 footOrigin   = transform.position + Vector3.up * 0.1f;
            Vector3 direction    = transform.forward;

            bool centerHit = Physics.Raycast(centerOrigin, direction,
                                 out RaycastHit centerRayHit,
                                 carStats.ObstacleCheckDistance,
                                 carStats.ObstacleMask);

            Vector3 footDirection = (direction + Vector3.up * 0.3f).normalized;
            bool footHit = Physics.Raycast(footOrigin, footDirection,
                               out RaycastHit footRayHit,
                               carStats.ObstacleCheckDistance * 0.7f,
                               carStats.ObstacleMask);

            Debug.DrawRay(centerOrigin, direction * carStats.ObstacleCheckDistance,
                          centerHit ? Color.red : Color.blue);
            Debug.DrawRay(footOrigin, footDirection * carStats.ObstacleCheckDistance * 0.7f,
                          footHit ? Color.red : Color.green);

            bool       didHit = centerHit || footHit;
            RaycastHit hit    = centerHit ? centerRayHit : footRayHit;

            if (!didHit)
                return new ObstacleAvoidanceResult { isAvoiding = false };

            float obstacleHeight = hit.point.y - transform.position.y;

            if (CanJumpOver(obstacleHeight))
            {
                botController.Jump();
                return new ObstacleAvoidanceResult { isAvoiding = false };
            }

            return NavigateAroundObstacle(centerOrigin);
        }
        
        private bool CanJumpOver(float obstacleHeight) =>
            obstacleHeight <= carStats.JumpableHeight && botController.IsGrounded();
        
        private ObstacleAvoidanceResult NavigateAroundObstacle(Vector3 origin)
        {
            bool leftClear  = !Physics.Raycast(origin, -transform.right, carStats.SideCheckDistance, carStats.ObstacleMask);
            bool rightClear = !Physics.Raycast(origin,  transform.right, carStats.SideCheckDistance, carStats.ObstacleMask);
            
            Debug.DrawRay(origin, -transform.right * carStats.SideCheckDistance, leftClear  ? Color.green : Color.red);
            Debug.DrawRay(origin,  transform.right * carStats.SideCheckDistance, rightClear ? Color.green : Color.red);
            
            float turnInput = DetermineTurnDirection(leftClear, rightClear);
            float moveInput = DetermineAvoidanceSpeed(leftClear, rightClear);
            
            return new ObstacleAvoidanceResult { isAvoiding = true, turnInput = turnInput, moveInput = moveInput };
        }
        
        private float DetermineTurnDirection(bool leftClear, bool rightClear)
        {
            if (leftClear  && !rightClear) return -carStats.AvoidanceTurnStrength;
            if (!leftClear && rightClear)  return  carStats.AvoidanceTurnStrength;
            return Random.value > 0.5f ? carStats.AvoidanceTurnStrength : -carStats.AvoidanceTurnStrength;
        }
        
        private float DetermineAvoidanceSpeed(bool leftClear, bool rightClear)
        {
            if (leftClear || rightClear) return 0.5f;
            return -0.3f;
        }
        
        private void RunAway()
        {
            if (lastAttacker == null) { TransitionToChase(); return; }
            
            ObstacleAvoidanceResult avoidance = HandleObstacleDetection();
            
            if (avoidance.isAvoiding)
                botController.SetInputs(avoidance.turnInput, avoidance.moveInput);
            else
                NavigateAwayFromAttacker();
            
            runAwayTimer += Time.deltaTime;
            if (runAwayTimer >= RUN_AWAY_DURATION)
                TransitionToChase();
        }
        
        private void NavigateAwayFromAttacker()
        {
            Vector3 awayDirection = (transform.position - lastAttacker.position).normalized;
            float   turnAmount    = CalculateRunAwayTurnAmount(awayDirection);
            botController.SetInputs(turnAmount, 1f);
        }
        
        private float CalculateRunAwayTurnAmount(Vector3 awayDirection)
        {
            float angle = Vector3.SignedAngle(transform.forward, awayDirection, Vector3.up);
            return Mathf.Clamp(angle / TURN_ANGLE_DIVISOR, -1f, 1f);
        }

        // ═══════════════════════════════════════════════
        //  SHOOTING
        // ═══════════════════════════════════════════════
        
        private void HandleShooting()
        {
            RegenerateCharge();
            
            if (currentState == BotStates.Attack && CanShoot())
            {
                Shoot();
                ConsumeCharge();
                nextFireTime = Time.time + fireRate;
            }
        }
        
        private void RegenerateCharge()
        {
            if (currentCharge >= MAX_CHARGE) return;
            currentCharge += chargeRegenRate * Time.fixedDeltaTime;
            currentCharge  = Mathf.Min(currentCharge, MAX_CHARGE);
            if (isOverheated && currentCharge >= MAX_CHARGE)
                isOverheated = false;
        }
        
        private void ConsumeCharge()
        {
            currentCharge -= CHARGE_PER_SHOT;
            if (currentCharge <= 0f) { currentCharge = 0f; isOverheated = true; }
        }
        
        private bool CanShoot() =>
            !isOverheated && Time.time > nextFireTime && currentCharge >= CHARGE_PER_SHOT;
        
        private void Shoot()
        {
            Vector3 dir = GetShootDirection();
            if (dir == Vector3.zero) return;
            GameObject bullet = InstantiateBullet(dir);
            ConfigureBullet(bullet, dir);
        }
        
        private Vector3 GetShootDirection()
        {
            if (target == null || firePoint == null) return Vector3.zero;
            return (target.position - firePoint.position).normalized;
        }
        
        private GameObject InstantiateBullet(Vector3 dir) =>
            Instantiate(projectile.ProjectilePrefab, firePoint.position, Quaternion.LookRotation(dir));
        
        private void ConfigureBullet(GameObject bullet, Vector3 dir)
        {
            var proj = bullet.GetComponent<Projectile>();
            if (proj != null) proj.SetShooter(gameObject);
            
            var bulletRb = bullet.GetComponent<Rigidbody>();
            if (bulletRb != null && botRigidbody != null)
            {
                bulletRb.linearVelocity = botRigidbody.linearVelocity;
                bulletRb.AddForce(dir * projectile.FireForce, ForceMode.Impulse);
            }
        }

        // ═══════════════════════════════════════════════
        //  HELPERS
        // ═══════════════════════════════════════════════
        
        private bool    IsTargetToTheRight() => GetTurnAngle() > 0;
        private float   GetTurnAngle()       => Vector3.SignedAngle(transform.forward, GetTargetDirection(), Vector3.up);
        private bool    IsTargetInFront()    => Vector3.Dot(transform.forward, GetTargetDirection()) > 0;
        
        private Vector3 GetTargetDirection()
        {
            if (target == null) return transform.forward;
            return (target.position - transform.position).normalized;
        }
        
        private float GetDistanceFromTarget()
        {
            if (target == null) return Mathf.Infinity;
            return Vector3.Distance(target.position, transform.position);
        }
        
        private void FindClosestTarget()
        {
            float     closestDistance = Mathf.Infinity;
            Transform closestTarget   = null;
            
            CarHealth[] allCars = FindObjectsByType<CarHealth>(FindObjectsSortMode.None);
            
            foreach (CarHealth car in allCars)
            {
                if (car.gameObject == gameObject) continue;
                float distance = Vector3.Distance(transform.position, car.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestTarget   = car.transform;
                }
            }
            
            if (closestTarget != null)
                target = closestTarget;
        }
        
        private void SetState(BotStates newState) => currentState = newState;
        
        public void OnHit(Transform attacker)
        {
            lastAttacker = attacker;
            runAwayTimer = 0f;
            SetState(BotStates.RunAway);
        }

        public void SetProjectile(ProjectileObject newProjectile)
        {
            if (newProjectile == null) return;
            projectile    = newProjectile;
            currentCharge = MAX_CHARGE;
            isOverheated  = false;
            nextFireTime  = 0f;
            InitializeFromProjectile();
            Debug.Log($"[{nameof(BotAI)}] {gameObject.name} swapped to {newProjectile.ProjectileName}");
        }

        public Vector3 GetAimDirection()
        {
            if (target == null || firePoint == null) return Vector3.zero;
            return (target.position - firePoint.position).normalized;
        }

        public bool IsFiring() => currentState == BotStates.Attack && CanShoot();
    }
}