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
        
        private PlayerInput playerInput;
        private InputAction moveAction;
        private InputAction jumpAction;
        private InputAction pauseAction;
        
        private Vector2 moveInput;
        private Rigidbody carRb;
        private PauseController pauseController;
        
        private const float GROUNDED_ANGULAR_DAMPING = 3f;
        private const float AIRBORNE_ANGULAR_DAMPING = 5f;
        private const float AIRBORNE_ROTATION_SPEED = 2f;
        private const float MAX_JUMP_HEIGHT_VELOCITY = 6f;
        
        private void Awake()
        {
            InitializeComponents();
        }
        
        private void Update()
        {
            HandlePauseInput();
        }
        
        private void OnEnable()
        {
            EnableInputActions();
            SubscribeToInputEvents();
        }
        
        private void OnDisable()
        {
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
            
            ApplyPhysics();
            ApplyMovementLimits();
        }
        
        private bool CanMove()
        {
            // Can't move if gameplay hasn't started yet
            if (GameFlowController.instance != null && !GameFlowController.instance.IsGameplayActive())
            {
                return false;
            }
            
            return true;
        }
        
        private void InitializeComponents()
        {
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
            }
        }
        
        private void InitializeRigidbody()
        {
            carRb = GetComponent<Rigidbody>();
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
            // Can't pause if game has ended
            if (GameplayManager.instance != null && GameplayManager.instance.IsGameEnded())
            {
                return false;
            }
            
            // Can't pause if gameplay hasn't started yet (during countdown)
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
            Vector3 force = direction * carStats.Acceleration;
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
            Vector3 torque = Vector3.up * turnDirection * carStats.TurnSpeed;
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
            
            Vector3 jumpForce = transform.up * carStats.JumpForce;
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
            
            if (velocity.y > MAX_JUMP_HEIGHT_VELOCITY)
            {
                velocity.y = MAX_JUMP_HEIGHT_VELOCITY;
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
                carRb.linearVelocity = carRb.linearVelocity.normalized * carStats.MaxSpeed;
            }
        }
        
        private bool IsExceedingMaxSpeed()
        {
            return carRb.linearVelocity.magnitude > carStats.MaxSpeed;
        }
    }
}