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
        private Rigidbody botRigidbody;
        private Transform firePoint;
        private CooldownBarUI cooldownBar;
        
        private BotStates currentState = BotStates.Chase;
        private Transform target;
        private Transform lastAttacker;
        
        private float nextFireTime = 0f;
        private float targetCheckTimer = 0f;
        private float runAwayTimer = 0f;
        
        private const float TARGET_CHECK_INTERVAL  = 2f;
        private const float RUN_AWAY_DURATION      = 3f;
        private const float RAYCAST_ORIGIN_HEIGHT  = 0.5f;
        private const float TURN_ANGLE_DIVISOR     = 45f;

        // ── Improved stuck detection ──
        private const float STUCK_CHECK_INTERVAL      = 0.5f;  // check more frequently
        private const float STUCK_MOVEMENT_THRESHOLD  = 0.5f;  // lower threshold
        private const float STUCK_SPEED_THRESHOLD     = 0.8f;  // speed threshold
        private const float REVERSE_DURATION          = 1.2f;
        private const float UNSTUCK_TURN_STRENGTH     = 0.8f;  // turn while reversing

        private float currentCharge;
        private const float MAX_CHARGE    = 100f;
        private const float CHARGE_PER_SHOT = 10f;
        private float chargeRegenRate;
        private float fireRate;
        private bool isOverheated = false;
        
        private Vector3   lastPosition;
        private float     stuckCheckTimer = 0f;
        private float     reverseTimer    = 0f;
        private bool      isReversing     = false;
        private float     unstuckTurnDir  = 1f; // which way to turn when unstucking
        private int       stuckCount      = 0;  // how many times stuck in a row

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
        }

        private void InitializeFromProjectile()
        {
            if (projectile == null) return;
            fireRate        = projectile.FireRate;
            chargeRegenRate = MAX_CHARGE / Mathf.Max(projectile.CooldownDuration, 0.1f);
        }
        
        private void InitializeComponents()
        {
            botController  = GetComponent<BotController>();
            botRigidbody   = GetComponent<Rigidbody>();
            firePoint      = transform.Find("FirePoint");
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
            cooldownBar.UpdateCooldown(currentCharge, MAX_CHARGE);
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
        //  IMPROVED STUCK DETECTION
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
                    stuckCount = 0; // reset if moving freely
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

            // Alternate turn direction each time stuck to avoid getting trapped in loops
            // If stuck multiple times in a row, turn more aggressively
            unstuckTurnDir = (stuckCount % 2 == 0) ? 1f : -1f;

            // Also check which side has more space and prefer that direction
            Vector3 origin = transform.position + Vector3.up * RAYCAST_ORIGIN_HEIGHT;
            bool leftClear  = !Physics.Raycast(origin, -transform.right, carStats.SideCheckDistance, carStats.ObstacleMask);
            bool rightClear = !Physics.Raycast(origin,  transform.right, carStats.SideCheckDistance, carStats.ObstacleMask);

            if (leftClear && !rightClear)  unstuckTurnDir = -1f;
            if (!leftClear && rightClear)  unstuckTurnDir =  1f;

            Debug.Log($"[BotAI] {gameObject.name} stuck! Reversing with turn {unstuckTurnDir} (stuck count: {stuckCount})");
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
            // Reverse AND turn — much better at getting unstuck than just reversing
            botController.SetInputs(unstuckTurnDir * UNSTUCK_TURN_STRENGTH, -1f);
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
            if (ReachedTarget()) return 0f;
            if (!IsTargetInFront()) return 0f;
            if (ShouldBrake()) return -1f;
            return 1f;
        }
        
        private bool ShouldBrake() =>
            GetDistanceFromTarget() < carStats.StoppingDistance &&
            botController.GetSpeed() > carStats.StoppingSpeed;
        
        private struct ObstacleAvoidanceResult
        {
            public bool  isAvoiding;
            public float turnInput;
            public float moveInput;
        }
        
        private ObstacleAvoidanceResult HandleObstacleDetection()
        {
            Vector3 origin    = transform.position + Vector3.up * RAYCAST_ORIGIN_HEIGHT;
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
        
        private bool CanJumpOver(float obstacleHeight) =>
            obstacleHeight <= carStats.JumpableHeight && botController.IsGrounded();
        
        private ObstacleAvoidanceResult NavigateAroundObstacle(Vector3 origin)
        {
            bool leftClear  = !Physics.Raycast(origin, -transform.right, carStats.SideCheckDistance, carStats.ObstacleMask);
            bool rightClear = !Physics.Raycast(origin,  transform.right, carStats.SideCheckDistance, carStats.ObstacleMask);
            
            Debug.DrawRay(origin, -transform.right * carStats.SideCheckDistance, leftClear  ? Color.green : Color.red);
            Debug.DrawRay(origin,  transform.right * carStats.SideCheckDistance, rightClear ? Color.green : Color.red);
            
            float turnInput  = DetermineTurnDirection(leftClear, rightClear);
            float moveInput  = DetermineAvoidanceSpeed(leftClear, rightClear);
            
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
            Vector3 shootDirection = GetShootDirection();
            if (shootDirection == Vector3.zero) return;

            GameObject bullet = InstantiateBullet(shootDirection);
            ConfigureBullet(bullet, shootDirection);
        }
        
        private Vector3 GetShootDirection()
        {
            if (target == null || firePoint == null) return Vector3.zero;
            return (target.position - firePoint.position).normalized;
        }
        
        private GameObject InstantiateBullet(Vector3 shootDirection) =>
            Instantiate(projectile.ProjectilePrefab, firePoint.position, Quaternion.LookRotation(shootDirection));
        
        private void ConfigureBullet(GameObject bullet, Vector3 shootDirection)
        {
            var proj = bullet.GetComponent<Projectile>();
            if (proj != null) proj.SetShooter(gameObject);
            
            var bulletRb = bullet.GetComponent<Rigidbody>();
            if (bulletRb != null && botRigidbody != null)
            {
                bulletRb.linearVelocity = botRigidbody.linearVelocity;
                bulletRb.AddForce(shootDirection * projectile.FireForce, ForceMode.Impulse);
            }
        }

        // ═══════════════════════════════════════════════
        //  HELPERS
        // ═══════════════════════════════════════════════
        
        private bool IsTargetToTheRight()  => GetTurnAngle() > 0;
        private float GetTurnAngle()        => Vector3.SignedAngle(transform.forward, GetTargetDirection(), Vector3.up);
        private bool IsTargetInFront()     => Vector3.Dot(transform.forward, GetTargetDirection()) > 0;
        
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
        }
    }
}