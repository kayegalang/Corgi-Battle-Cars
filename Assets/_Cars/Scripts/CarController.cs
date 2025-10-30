using Player.Scripts;
using UI.Scripts;
using UnityEngine;

namespace _Cars.Scripts
{
    public class CarController : MonoBehaviour
    {
        private PlayerControls controls;
        private Vector2 moveInput;
        private Rigidbody carRb;
        
        private PauseController pauseController;
        
        [SerializeField] private CarStats carStats;

        void Awake()
        {
            controls = new PlayerControls();
            carRb = GetComponent<Rigidbody>();
            
            pauseController = FindFirstObjectByType<PauseController>();
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

            if (carRb.linearVelocity.magnitude > carStats.maxSpeed)
            {
                carRb.linearVelocity = carRb.linearVelocity.normalized * carStats.maxSpeed;
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
                    carRb.AddTorque(Vector3.up * carStats.turnSpeed);
                }
                else if (moveInput.x < 0)
                {
                    carRb.AddTorque(-Vector3.up * carStats.turnSpeed);
                }
            }
            else if (!IsMovingForward())
            {
                if (moveInput.x > 0)
                {
                    carRb.AddTorque(-Vector3.up * carStats.turnSpeed);
                }
                else if (moveInput.x < 0)
                {
                    carRb.AddTorque(Vector3.up * carStats.turnSpeed);
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
            {
                carRb.AddForce(transform.up * carStats.jumpForce, ForceMode.Impulse);
            }
        }
    }
} 