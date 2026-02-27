using System.Collections;
using _Cars.ScriptableObjects;
using _UI.Scripts;
using _Gameplay.Scripts;
using UnityEngine;
using UnityEngine.InputSystem;

namespace _Cars.Scripts
{
    public class CarController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private CarStats carStats;
        
        [Header("Zoomies VFX")]
        [SerializeField] private ParticleSystem zoomiesParticles;
        
        private PlayerInput playerInput;
        private InputAction moveAction;
        private InputAction jumpAction;
        private InputAction pauseAction;
        
        private Vector2 moveInput;
        private Rigidbody carRb;
        private PauseController pauseController;
        
        // Zoomies power-up state
        private bool hasZoomies = false;
        private float speedMultiplier = 1f;
        private float accelerationMultiplier = 1f;
        
        // Super Jump power-up state
        private bool hasSuperJump = false;
        private float jumpMultiplier = 1f;
        private float jumpHeightCapMultiplier = 1f;
        
        // Poop power-up state
        private bool isSlipping = false;
        private Coroutine slipCoroutine;
        
        private const float GROUNDED_ANGULAR_DAMPING = 3f;
        private const float AIRBORNE_ANGULAR_DAMPING = 5f;
        private const float AIRBORNE_ROTATION_SPEED = 2f;
        private const float MAX_JUMP_HEIGHT_VELOCITY = 6f;
        private const float AIR_CONTROL_FACTOR = 0.3f; 

        
        private void Awake()
        {
            Debug.Log($"[CarController] Awake called on {gameObject.name}");
            InitializeComponents();
        }
        
        private void Update()
        {
            HandlePauseInput();
        }
        
        private void OnEnable()
        {
            Debug.Log($"[CarController] OnEnable called on {gameObject.name}");
            EnableInputActions();
            SubscribeToInputEvents();
        }
        
        private void OnDisable()
        {
            Debug.Log($"[CarController] OnDisable called on {gameObject.name}");
            DisableInputActions();
            UnsubscribeFromInputEvents();
        }
        
        private void FixedUpdate()
        {
            // Don't allow movement until gameplay has started
            if (!CanMove())
            {
                return;
            }
            
            // Can't move while slipping!
            if (isSlipping)
            {
                Debug.Log($"[CarController] {gameObject.name} is currently slipping - no player input!");
                ApplyMovementLimits();
                return;
            }
            
            ApplyPhysics();
            ApplyMovementLimits();
        }
        
        private bool CanMove()
        {
            if (GameFlowController.instance != null && !GameFlowController.instance.IsGameplayActive())
            {
                return false;
            }
            
            return true;
        }
        
        private void InitializeComponents()
        {
            Debug.Log($"[CarController] Initializing components on {gameObject.name}");
            InitializePlayerInput();
            InitializeRigidbody();
            InitializePauseController();
            ValidateComponents();
        }
        
        private void InitializePlayerInput()
        {
            playerInput = GetComponent<PlayerInput>();
            
            if (playerInput != null)
            {
                var actions = playerInput.actions;
                moveAction = actions.FindAction("Move", true);
                jumpAction = actions.FindAction("Jump", true);
                pauseAction = actions.FindAction("Pause", true);
                Debug.Log($"[CarController] PlayerInput initialized successfully on {gameObject.name}");
            }
        }
        
        private void InitializeRigidbody()
        {
            carRb = GetComponent<Rigidbody>();
            if (carRb != null)
            {
                Debug.Log($"[CarController] Rigidbody found on {gameObject.name}");
                Debug.Log($"[CarController] Rigidbody IsKinematic: {carRb.isKinematic}");
                Debug.Log($"[CarController] Rigidbody Constraints: {carRb.constraints}");
            }
        }
        
        private void InitializePauseController()
        {
            pauseController = FindFirstObjectByType<PauseController>();
        }
        
        private void ValidateComponents()
        {
            if (playerInput == null)
            {
                Debug.LogError($"[{nameof(CarController)}] PlayerInput not found on {gameObject.name}!");
            }
            
            if (carRb == null)
            {
                Debug.LogError($"[{nameof(CarController)}] Rigidbody not found on {gameObject.name}!");
            }
            
            if (carStats == null)
            {
                Debug.LogError($"[{nameof(CarController)}] CarStats not assigned on {gameObject.name}!");
            }
            
            Debug.Log($"[CarController] Component validation complete on {gameObject.name}");
        }
        
        private void HandlePauseInput()
        {
            if (!CanPause())
            {
                return;
            }
            
            if (pauseAction.triggered)
            {
                TogglePause();
            }
        }
        
        private bool CanPause()
        {
            if (GameplayManager.instance != null && GameplayManager.instance.IsGameEnded())
            {
                return false;
            }
            
            if (GameFlowController.instance != null && !GameFlowController.instance.IsGameplayActive())
            {
                return false;
            }
            
            return true;
        }
        
        private void TogglePause()
        {
            if (pauseController == null)
            {
                return;
            }
            
            if (pauseController.GetIsPaused())
            {
                pauseController.UnpauseGame();
            }
            else
            {
                pauseController.PauseGame();
            }
        }
        
        private void EnableInputActions()
        {
            moveAction?.Enable();
            jumpAction?.Enable();
            pauseAction?.Enable();
        }
        
        private void DisableInputActions()
        {
            moveAction?.Disable();
            jumpAction?.Disable();
            pauseAction?.Disable();
        }
        
        private void SubscribeToInputEvents()
        {
            if (moveAction != null)
            {
                moveAction.performed += OnMovePerformed;
                moveAction.canceled += OnMoveCanceled;
            }
            
            if (jumpAction != null)
            {
                jumpAction.performed += OnJumpPerformed;
            }
        }
        
        private void UnsubscribeFromInputEvents()
        {
            if (moveAction != null)
            {
                moveAction.performed -= OnMovePerformed;
                moveAction.canceled -= OnMoveCanceled;
            }
            
            if (jumpAction != null)
            {
                jumpAction.performed -= OnJumpPerformed;
            }
        }
        
        private void OnMovePerformed(InputAction.CallbackContext ctx)
        {
            moveInput = ctx.ReadValue<Vector2>();
        }
        
        private void OnMoveCanceled(InputAction.CallbackContext ctx)
        {
            moveInput = Vector2.zero;
        }
        
        private void OnJumpPerformed(InputAction.CallbackContext ctx)
        {
            Jump();
        }
        
        private void ApplyPhysics()
        {
            Move();
            Turn();
            ApplyAngularDamping();
            LevelRotationInAir();
        }
        
        private void ApplyMovementLimits()
        {
            CapJumpHeight();
            CapMaxSpeed();
        }
        
        private void Move()
        {
            if (!ShouldMove())
            {
                ConstrainLateralMovement();
                return;
            }
            
            ApplyAcceleration();
            ConstrainLateralMovement();
        }
        
        private bool ShouldMove()
        {
            return moveInput.y != 0;
        }
        
        private void ApplyAcceleration()
        {
            Vector3 direction = moveInput.y > 0 ? Vector3.forward : -Vector3.forward;
            Vector3 force = direction * carStats.Acceleration * accelerationMultiplier;
            carRb.AddRelativeForce(force);
        }
        
        private void ConstrainLateralMovement()
        {
            Vector3 localVelocity = transform.InverseTransformDirection(carRb.linearVelocity);
            localVelocity.x = 0;
            carRb.linearVelocity = transform.TransformDirection(localVelocity);
        }
        
        private void Turn()
        {
            if (!ShouldTurn())
            {
                return;
            }
            float turnDirection = IsMovingForward() ? moveInput.x : -moveInput.x;
                
            float controlFactor = IsGrounded() ? 1f : AIR_CONTROL_FACTOR;
               
            Vector3 torque = Vector3.up * turnDirection * carStats.TurnSpeed * controlFactor;
            carRb.AddTorque(torque);
        }
        
        private bool ShouldTurn()
        {
            return moveInput.x != 0;
        }
        
        private bool IsMovingForward()
        {
            return moveInput.y >= 0;
        }
        
        private void ApplyAngularDamping()
        {
            carRb.angularDamping = IsGrounded() ? GROUNDED_ANGULAR_DAMPING : AIRBORNE_ANGULAR_DAMPING;
        }
        
        private void LevelRotationInAir()
        {
            if (IsGrounded())
            {
                return;
            }
            
            Quaternion levelRotation = Quaternion.Euler(0, carRb.rotation.eulerAngles.y, 0);
            Quaternion newRotation = Quaternion.Slerp(
                carRb.rotation, 
                levelRotation, 
                AIRBORNE_ROTATION_SPEED * Time.fixedDeltaTime
            );
            
            carRb.MoveRotation(newRotation);
        }
        
        private bool IsGrounded()
        {
            if (carStats == null)
            {
                return false;
            }
            
            Vector3 origin = transform.position + carStats.GroundCheckOffset;
            Vector3 direction = -transform.up;
            float distance = carStats.GroundCheckDistance;
            
            Debug.DrawRay(origin, direction * distance, Color.red);
            
            return Physics.Raycast(origin, direction, out RaycastHit hit, distance);
        }
        
        private void Jump()
        {
            if (!CanJump())
            {
                return;
            }
            
            Vector3 jumpForce = transform.up * carStats.JumpForce * jumpMultiplier;
            carRb.AddForce(jumpForce, ForceMode.Impulse);
        }
        
        private bool CanJump()
        {
            return IsGrounded() && carStats != null;
        }
        
        private void CapJumpHeight()
        {
            if (carRb == null)
            {
                return;
            }
            
            Vector3 velocity = carRb.linearVelocity;
            
            if (velocity.y > MAX_JUMP_HEIGHT_VELOCITY * jumpHeightCapMultiplier)
            {
                velocity.y = MAX_JUMP_HEIGHT_VELOCITY * jumpHeightCapMultiplier;
                carRb.linearVelocity = velocity;
            }
        }
        
        private void CapMaxSpeed()
        {
            if (carRb == null || carStats == null)
            {
                return;
            }
            
            if (IsExceedingMaxSpeed())
            {
                carRb.linearVelocity = carRb.linearVelocity.normalized * (carStats.MaxSpeed * speedMultiplier);
            }
        }
        
        private bool IsExceedingMaxSpeed()
        {
            return carRb.linearVelocity.magnitude > (carStats.MaxSpeed * speedMultiplier);
        }
        
        // ═══════════════════════════════════════════════
        //  ZOOMIES POWER-UP
        // ═══════════════════════════════════════════════
        
        public void ApplySpeedMultiplier(float speedMult, float accelMult)
        {
            hasZoomies = true;
            speedMultiplier = speedMult;
            accelerationMultiplier = accelMult;
            
            if (zoomiesParticles != null)
            {
                zoomiesParticles.Play();
                Debug.Log($"[CarController] Zoomies particles started on {gameObject.name}");
            }
            
            Debug.Log($"[CarController] {gameObject.name} got ZOOMIES! Speed x{speedMult}, Accel x{accelMult} ⚡");
        }
        
        public void RemoveSpeedMultiplier()
        {
            hasZoomies = false;
            speedMultiplier = 1f;
            accelerationMultiplier = 1f;
            
            if (zoomiesParticles != null)
            {
                zoomiesParticles.Stop();
                Debug.Log($"[CarController] Zoomies particles stopped on {gameObject.name}");
            }
            
            Debug.Log($"[CarController] {gameObject.name}'s zoomies wore off!");
        }
        
        // ═══════════════════════════════════════════════
        //  SUPER JUMP POWER-UP
        // ═══════════════════════════════════════════════
        
        public void ApplyJumpMultiplier(float jumpMult, float jumpHeightCapMult)
        {
            hasSuperJump = true;
            jumpMultiplier = jumpMult;
            jumpHeightCapMultiplier = jumpHeightCapMult;
            
            Debug.Log($"[CarController] {gameObject.name} got SUPER JUMP! Jump x{jumpMult}, Height Cap x{jumpHeightCapMult} 🚀");
        }
        
        public void RemoveJumpMultiplier()
        {
            hasSuperJump = false;
            jumpMultiplier = 1f;
            jumpHeightCapMultiplier = 1f;
            
            Debug.Log($"[CarController] {gameObject.name}'s super jump wore off!");
        }
        
        // ═══════════════════════════════════════════════
        //  POOP POWER-UP - SLIP MECHANIC
        // ═══════════════════════════════════════════════
        
        public void TriggerSlip(float duration, float spinForce)
        {
            Debug.Log($"[CarController] TriggerSlip called on {gameObject.name}! Duration: {duration}, SpinForce: {spinForce}");
            
            if (isSlipping)
            {
                Debug.Log($"[CarController] {gameObject.name} is already slipping - ignoring new slip trigger");
                return;
            }
            
            if (carRb == null)
            {
                Debug.LogError($"[CarController] Cannot trigger slip - Rigidbody is null on {gameObject.name}!");
                return;
            }
            
            Debug.Log($"[CarController] Starting slip coroutine on {gameObject.name}");
            
            if (slipCoroutine != null)
            {
                Debug.Log($"[CarController] Stopping existing slip coroutine");
                StopCoroutine(slipCoroutine);
            }
            
            slipCoroutine = StartCoroutine(SlipRoutine(duration, spinForce));
        }
        
        private IEnumerator SlipRoutine(float duration, float spinForce)
        {
            Debug.Log($"[CarController] SlipRoutine STARTED on {gameObject.name}");
            
            isSlipping = true;
            Debug.Log($"[CarController] isSlipping set to TRUE");
            
            Debug.Log($"[CarController] {gameObject.name} is slipping! 💩💨");
            
            SpinPlayer(spinForce);
            
            Debug.Log($"[CarController] Waiting {duration} seconds...");
            yield return new WaitForSeconds(duration);
            
            isSlipping = false;
            Debug.Log($"[CarController] isSlipping set to FALSE");
            
            Debug.Log($"[CarController] {gameObject.name} regained control! SlipRoutine ENDED");
        }

        private void SpinPlayer(float spinForce)
        {
            Debug.Log($"[CarController] SpinPlayer called with force: {spinForce}");
            
            if (carRb == null)
            {
                Debug.LogError($"[CarController] Cannot spin - Rigidbody is null!");
                return;
            }
            
            // Check Rigidbody constraints
            Debug.Log($"[CarController] Rigidbody constraints: {carRb.constraints}");
            
            float randomDirection = Random.value > 0.5f ? 1f : -1f;
            Debug.Log($"[CarController] Random spin direction: {randomDirection}");
            
            Vector3 spinTorque = Vector3.up * spinForce * randomDirection;
            Debug.Log($"[CarController] Applying spin torque: {spinTorque}");
            
            carRb.AddTorque(spinTorque, ForceMode.Impulse);
            
            Debug.Log($"[CarController] Torque applied! Current angular velocity: {carRb.angularVelocity}");
        }
    }
}