using Player.Scripts;
using UI.Scripts;
using UnityEngine;
using UnityEngine.InputSystem;

namespace _Cars.Scripts
{
    public class CarController : MonoBehaviour
    {
        private PlayerInput playerInput;

        private InputAction moveAction;
        private InputAction jumpAction;
        private InputAction pauseAction;

        private Vector2 moveInput;
        private Rigidbody carRb;

        private PauseController pauseController;

        [SerializeField] private CarStats carStats;

        private bool isInitialized = false;

        void Awake()
        {
            playerInput = GetComponent<PlayerInput>();
            carRb = GetComponent<Rigidbody>();
            pauseController = FindFirstObjectByType<PauseController>();
        }

        void Start()
        {
            // Initialize actions in Start() instead of Awake()
            // This gives PlayerInput time to fully initialize
            try
            {
                if (playerInput != null && playerInput.enabled)
                {
                    InitializeActions();
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"Failed to initialize actions on {gameObject.name}: {e.Message}");
            }
        }

        private void InitializeActions()
        {
            if (isInitialized) return;
            
            // Add null check for PlayerInput component
            if (playerInput == null)
            {
                Debug.LogWarning($"PlayerInput is null on {gameObject.name}");
                return;
            }

            // Add null check for actions
            if (playerInput.actions == null)
            {
                Debug.LogWarning($"PlayerInput actions are null on {gameObject.name}");
                return;
            }

            var actions = playerInput.actions;
            moveAction  = actions.FindAction("Move",  true);
            jumpAction  = actions.FindAction("Jump",  true);
            pauseAction = actions.FindAction("Pause", true);

            // Verify actions were found
            if (moveAction == null || jumpAction == null || pauseAction == null)
            {
                Debug.LogWarning($"Some actions not found on {gameObject.name}");
                return;
            }

            isInitialized = true;
        }

        private void OnEnable()
        {
            // Initialize actions when enabled (handles re-enabling after being disabled)
            if (!isInitialized && playerInput != null && playerInput.enabled)
            {
                InitializeActions();
            }

            // Only set up callbacks if actions are initialized
            if (isInitialized && moveAction != null && jumpAction != null && pauseAction != null)
            {
                moveAction.Enable();
                jumpAction.Enable();
                pauseAction.Enable();

                moveAction.performed += OnMovePerformed;
                moveAction.canceled  += OnMoveCanceled;

                jumpAction.performed += OnJumpPerformed;

                pauseAction.performed += OnPausePerformed;
            }
        }

        private void OnDisable()
        {
            // Only clean up if actions were initialized
            if (isInitialized && moveAction != null && jumpAction != null && pauseAction != null)
            {
                // Remove ALL callbacks safely
                moveAction.performed -= OnMovePerformed;
                moveAction.canceled  -= OnMoveCanceled;

                jumpAction.performed -= OnJumpPerformed;
                pauseAction.performed -= OnPausePerformed;

                moveAction.Disable();
                jumpAction.Disable();
                pauseAction.Disable();
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

        private void OnPausePerformed(InputAction.CallbackContext ctx)
        {
            if (pauseController == null) return;

            if (pauseController.GetIsPaused())
                pauseController.UnpauseGame();
            else
                pauseController.PauseGame();
        }

        private void FixedUpdate()
        {
            if (this == null || carRb == null) return;

            CapJumpHeight();
            Move();
            Turn();

            if (IsGrounded())
            {
                carRb.angularDamping = 3;
            }
            else
            {
                carRb.angularDamping = 5;

                Quaternion levelRotation = Quaternion.Euler(0, carRb.rotation.eulerAngles.y, 0);
                carRb.MoveRotation(Quaternion.Slerp(carRb.rotation, levelRotation, 2f * Time.fixedDeltaTime));
            }

            if (carRb.linearVelocity.magnitude > carStats.maxSpeed)
            {
                carRb.linearVelocity = carRb.linearVelocity.normalized * carStats.maxSpeed;
            }
        }
        
        private void Move()
        {
            if (moveInput.y > 0)
            {
                carRb.AddRelativeForce(Vector3.forward * carStats.acceleration);
            }
            else if (moveInput.y < 0)
            {
                carRb.AddRelativeForce(-Vector3.forward * carStats.acceleration);
            }

            Vector3 localVelocity = transform.InverseTransformDirection(carRb.linearVelocity);
            localVelocity.x = 0;
            carRb.linearVelocity = transform.TransformDirection(localVelocity);
        }

        private void Turn()
        {
            if (IsMovingForward())
            {
                if (moveInput.x > 0)
                    carRb.AddTorque(Vector3.up * carStats.turnSpeed);
                else if (moveInput.x < 0)
                    carRb.AddTorque(-Vector3.up * carStats.turnSpeed);
            }
            else
            {
                if (moveInput.x > 0)
                    carRb.AddTorque(-Vector3.up * carStats.turnSpeed);
                else if (moveInput.x < 0)
                    carRb.AddTorque(Vector3.up * carStats.turnSpeed);
            }
        }

        private bool IsMovingForward()
        {
            return moveInput.y >= 0;
        }
        
        private bool IsGrounded()
        {
            RaycastHit hit;

            Vector3 origin = transform.position + carStats.groundCheckOffset;

            Debug.DrawRay(origin, -transform.up * carStats.groundCheckDistance, Color.red);

            return Physics.Raycast(origin, -transform.up, out hit, carStats.groundCheckDistance);
        }

        private void Jump()
        {
            if (IsGrounded())
                carRb.AddForce(transform.up * carStats.jumpForce, ForceMode.Impulse);
        }

        private void CapJumpHeight()
        {
            Vector3 vel = carRb.linearVelocity;
            if (vel.y > 6f)
            {
                vel.y = 6f;
                carRb.linearVelocity = vel;
            }
        }
    }
}