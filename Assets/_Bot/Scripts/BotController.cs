using _Cars.ScriptableObjects;
using UnityEngine;

namespace _Bot.Scripts
{
    public class BotController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private CarStats carStats;
        
        [Header("Zoomies VFX")]
        [SerializeField] private ParticleSystem zoomiesParticles; 
        
        private Vector2 moveInput;
        private Rigidbody carRb;
        
        // Zoomies power-up state
        private bool hasZoomies = false;
        private float speedMultiplier = 1f;
        private float accelerationMultiplier = 1f;
        
        private const float GROUNDED_ANGULAR_DAMPING = 3f;
        private const float AIRBORNE_ANGULAR_DAMPING = 5f;
        private const float AIRBORNE_ROTATION_SPEED = 2f;
        private const float MOVE_INPUT_THRESHOLD = 0.01f;
        private const float MAX_JUMP_HEIGHT_VELOCITY = 6f;
        
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
            Vector3 force = Vector3.forward * moveInput.y * carStats.Acceleration * accelerationMultiplier;
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
            float turnDirection = IsMovingForward() ? 1f : -1f;
            Vector3 torque = Vector3.up * moveInput.x * carStats.TurnSpeed * turnDirection;
            carRb.AddTorque(torque);
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
                carRb.linearVelocity = carRb.linearVelocity.normalized * (carStats.MaxSpeed * speedMultiplier);
            }
        }
        
        private bool IsExceedingMaxSpeed()
        {
            return carRb.linearVelocity.magnitude > (carStats.MaxSpeed * speedMultiplier);
        }
        
        // ═══════════════════════════════════════════════
        //  ZOOMIES POWER-UP! ⚡🌟
        // ═══════════════════════════════════════════════
        
        public void ApplySpeedMultiplier(float speedMult, float accelMult)
        {
            hasZoomies = true;
            speedMultiplier = speedMult;
            accelerationMultiplier = accelMult;
            
            // Start the speed lines effect!
            if (zoomiesParticles != null)
            {
                zoomiesParticles.Play();
            }
            
            Debug.Log($"[BotController] {gameObject.name} got ZOOMIES! Speed x{speedMult}, Accel x{accelMult} ⚡");
        }
        
        public void RemoveSpeedMultiplier()
        {
            hasZoomies = false;
            speedMultiplier = 1f;
            accelerationMultiplier = 1f;
            
            // Stop the speed lines
            if (zoomiesParticles != null)
            {
                zoomiesParticles.Stop();
            }
            
            Debug.Log($"[BotController] {gameObject.name}'s zoomies wore off!");
        }
    }
}