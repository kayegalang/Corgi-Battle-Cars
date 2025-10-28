using Gameplay.Scripts;
using UI.Scripts;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Player.Scripts
{
    public class CarController : MonoBehaviour
    {
        private PlayerControls controls;
        private Vector2 moveInput;
        private Rigidbody carRb;
        
        private PauseController pauseController;
        
        [SerializeField] private CarStats carStats;
        
        private float acceleration;
        private float turnSpeed;
        private Vector3 groundCheckOffset;
        private float groundCheckDistance;
        private float jumpForce;
        private float maxSpeed;

        void Awake()
        {
            controls = new PlayerControls();
            carRb = GetComponent<Rigidbody>();
            
            pauseController = FindFirstObjectByType<PauseController>();

            InitializeCarStats();
        }


        void Update()
        {
            if (controls.UI.Pause.triggered) 
            {
                if (pauseController.GetIsPaused())
                {
                    pauseController.UnpauseGame();
                }
                else
                {
                    pauseController.PauseGame();
                }
            }

        }

        private void InitializeCarStats()
        {
            acceleration = carStats.acceleration;
            turnSpeed = carStats.turnSpeed;
            groundCheckOffset = carStats.groundCheckOffset;
            groundCheckDistance = carStats.groundCheckDistance;
            jumpForce = carStats.jumpForce;
            maxSpeed = carStats.maxSpeed;
        }

        private void OnEnable()
        {
            controls.Gameplay.Enable();
            controls.UI.Enable();
            
            // Movement
            controls.Gameplay.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
            controls.Gameplay.Move.canceled += ctx => moveInput = Vector2.zero;
            
            // Jump
            controls.Gameplay.Jump.performed += ctx => Jump();
        }

        private void OnDisable()
        {
            controls.Gameplay.Disable();
            controls.UI.Disable();
        }

        private void FixedUpdate()
        {
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

            if (carRb.linearVelocity.magnitude > maxSpeed)
            {
                carRb.linearVelocity = carRb.linearVelocity.normalized * maxSpeed;
            }
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

        private void Turn()
        {
            if (IsMovingForward())
            {
                if (moveInput.x > 0)
                {
                    carRb.AddTorque(Vector3.up * turnSpeed);
                }
                else if (moveInput.x < 0)
                {
                    carRb.AddTorque(-Vector3.up * turnSpeed);
                }
            }
            else if (!IsMovingForward())
            {
                if (moveInput.x > 0)
                {
                    carRb.AddTorque(-Vector3.up * turnSpeed);
                }
                else if (moveInput.x < 0)
                {
                    carRb.AddTorque(Vector3.up * turnSpeed);
                }
            }
        }

        private bool IsMoving()
        {
            return moveInput.y != 0;
        }

        private void Move()
        {
            if (moveInput.y > 0)
            {
                carRb.AddRelativeForce(Vector3.forward * acceleration);
            }
            else if (moveInput.y < 0)
            {
                carRb.AddRelativeForce(-Vector3.forward * acceleration);
            }

            Vector3 localVelocity = transform.InverseTransformDirection(carRb.linearVelocity);
            localVelocity.x = 0;
            carRb.linearVelocity = transform.TransformDirection(localVelocity);
        }

        private bool IsMovingForward()
        {
            return moveInput.y >= 0;
        }

        private bool IsGrounded()
        {
            RaycastHit hit;
            
            Vector3 origin = transform.position + groundCheckOffset;

            Debug.DrawRay(origin, -transform.up * groundCheckDistance, Color.red);
            
            return Physics.Raycast(origin, -transform.up, out hit, groundCheckDistance);
        }

        private void Jump()
        {
            if (IsGrounded())
            {
                carRb.AddForce(transform.up * jumpForce, ForceMode.Impulse);
            }
        }
    }
} 