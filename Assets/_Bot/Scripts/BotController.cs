using _Cars.ScriptableObjects;
using UnityEngine;

namespace _Bot.Scripts
{
    public class BotController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private CarStats carStats;
        
        private Vector2 moveInput;
        private Rigidbody carRb;
        
        private const float GROUNDED_ANGULAR_DAMPING = 3f;
        private const float AIRBORNE_ANGULAR_DAMPING = 5f;
        private const float AIRBORNE_ROTATION_SPEED = 2f;
        private const float MOVE_INPUT_THRESHOLD = 0.01f;
        
        private void Awake()
        {
            InitializeComponents();
        }
        
        private void FixedUpdate()
        {
            ApplyPhysics();
            ApplyMovementLimits();
        }
        
        private void InitializeComponents()
        {
            carRb = GetComponent<Rigidbody>();
            ValidateComponents();
        }
        
        private void ValidateComponents()
        {
            if (carRb == null)
            {
                Debug.LogError($"[{nameof(BotController)}] Rigidbody not found on {gameObject.name}!");
            }
            
            if (carStats == null)
            {
                Debug.LogError($"[{nameof(BotController)}] CarStats not assigned on {gameObject.name}!");
            }
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
            return Mathf.Abs(moveInput.y) > MOVE_INPUT_THRESHOLD;
        }
        
        private void ApplyAcceleration()
        {
            Vector3 force = Vector3.forward * moveInput.y * carStats.Acceleration;
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

            float turnDirection = IsMovingForward() ? 1f : -1f;
            Vector3 torque = Vector3.up * moveInput.x * carStats.TurnSpeed * turnDirection;
            carRb.AddTorque(torque);
        }
        
        private bool ShouldTurn()
        {
            return Mathf.Abs(moveInput.x) > MOVE_INPUT_THRESHOLD;
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
        
        public bool IsGrounded()
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
        
        public void Jump()
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
        
        public void SetInputs(float turnAmount, float moveAmount)
        {
            moveInput.x = Mathf.Clamp(turnAmount, -1f, 1f);
            moveInput.y = Mathf.Clamp(moveAmount, -1f, 1f);
        }
        
        public float GetSpeed()
        {
            if (carRb == null)
            {
                return 0f;
            }
            
            return carRb.linearVelocity.magnitude;
        }
        
        private void CapJumpHeight()
        {
            if (carRb == null || carStats == null)
            {
                return;
            }

            float maxJumpVelocity = carStats.JumpForce * 0.5f;
            
            Vector3 velocity = carRb.linearVelocity;
            
            if (velocity.y > maxJumpVelocity)
            {
                velocity.y = maxJumpVelocity;
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