using System.Collections;
using _Cars.ScriptableObjects;
using _Cars.Scripts;
using _Gameplay.Scripts;
using UnityEngine;

namespace _Bot.Scripts
{
    public class BotController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private CarStats carStats;
        
        [Header("Zoomies VFX")]
        [SerializeField] private ParticleSystem zoomiesParticles;

        [Header("Crash Settings")]
        [SerializeField] private int   crashDamage   = 5;
        [SerializeField] private float crashMinSpeed  = 3f;
        [SerializeField] private float crashCooldown  = 0.5f;

        [Header("Collision Physics")]
        [Tooltip("Clamp vertical velocity after collisions to prevent floating")]
        [SerializeField] private float maxVerticalVelocityAfterCollision = 2f;
        
        private Vector2    moveInput;
        private Rigidbody  carRb;

        private bool  hasZoomies             = false;
        private float speedMultiplier        = 1f;
        private float accelerationMultiplier = 1f;
        
        private bool  hasSuperJump            = false;
        private float jumpMultiplier          = 1f;
        private float jumpHeightCapMultiplier = 1f;
        
        private bool      isSlipping    = false;
        private Coroutine slipCoroutine;

        private float lastCrashTime = -999f;
        
        private const float GROUNDED_ANGULAR_DAMPING = 3f;
        private const float AIRBORNE_ANGULAR_DAMPING = 5f;
        private const float AIRBORNE_ROTATION_SPEED  = 2f;
        private const float MOVE_INPUT_THRESHOLD     = 0.01f;
        private const float MAX_JUMP_HEIGHT_VELOCITY = 6f;

        private void Awake()
        {
            InitializeComponents();
        }
        
        private void FixedUpdate()
        {
            if (GameFlowController.instance != null && !GameFlowController.instance.IsGameplayActive())
                return;

            if (isSlipping)
            {
                ApplyMovementLimits();
                return;
            }
    
            ApplyPhysics();
            ApplyMovementLimits();
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (GameFlowController.instance != null && !GameFlowController.instance.IsGameplayActive())
                return;

            // ── FIX: Clamp vertical velocity immediately to prevent floating ──
            if (carRb != null)
            {
                Vector3 vel = carRb.linearVelocity;
                if (vel.y > maxVerticalVelocityAfterCollision)
                {
                    vel.y                = maxVerticalVelocityAfterCollision;
                    carRb.linearVelocity = vel;
                }
            }

            if (Time.time - lastCrashTime < crashCooldown) return;

            Vector3 impactDirection = collision.GetContact(0).normal;
            if (Mathf.Abs(impactDirection.y) > 0.5f) return;

            float impactSpeed = collision.relativeVelocity.magnitude;
            if (impactSpeed < crashMinSpeed) return;

            lastCrashTime = Time.time;

            CarHealth myHealth = GetComponent<CarHealth>();
            myHealth?.TakeDamage(crashDamage, null);

            CarHealth theirHealth = collision.gameObject.GetComponent<CarHealth>();
            if (theirHealth != null)
                theirHealth.TakeDamage(crashDamage, null);
        }

        private void InitializeComponents()
        {
            carRb = GetComponent<Rigidbody>();
            ValidateComponents();
        }
        
        private void ValidateComponents()
        {
            if (carRb == null)
                Debug.LogError($"[{nameof(BotController)}] Rigidbody not found on {gameObject.name}!");
            if (carStats == null)
                Debug.LogError($"[{nameof(BotController)}] CarStats not assigned on {gameObject.name}!");
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
            if (!ShouldMove()) return;
            ApplyAcceleration();
            ConstrainLateralMovement();
        }
        
        private bool ShouldMove() => Mathf.Abs(moveInput.y) > MOVE_INPUT_THRESHOLD;
        
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
        
        private bool IsMovingForward() => moveInput.y >= 0;
        
        private void ApplyAngularDamping()
        {
            carRb.angularDamping = IsGrounded() ? GROUNDED_ANGULAR_DAMPING : AIRBORNE_ANGULAR_DAMPING;
        }
        
        private void LevelRotationInAir()
        {
            if (IsGrounded()) return;
            
            Quaternion levelRotation = Quaternion.Euler(0, carRb.rotation.eulerAngles.y, 0);
            Quaternion newRotation   = Quaternion.Slerp(
                carRb.rotation, 
                levelRotation, 
                AIRBORNE_ROTATION_SPEED * Time.fixedDeltaTime
            );
            carRb.MoveRotation(newRotation);
        }
        
        public bool IsGrounded()
        {
            if (carStats == null) return false;
            Vector3 origin    = transform.position + carStats.GroundCheckOffset;
            Vector3 direction = -transform.up;
            float   distance  = carStats.GroundCheckDistance;
            Debug.DrawRay(origin, direction * distance, Color.red);
            return Physics.Raycast(origin, direction, out RaycastHit hit, distance);
        }
        
        public void Jump()
        {
            if (!CanJump()) return;
            Vector3 jumpForce = transform.up * carStats.JumpForce * jumpMultiplier;
            carRb.AddForce(jumpForce, ForceMode.Impulse);
        }
        
        private bool CanJump() => IsGrounded() && carStats != null;
        
        public void SetInputs(float turnAmount, float moveAmount)
        {
            moveInput.x = Mathf.Clamp(turnAmount, -1f, 1f);
            moveInput.y = Mathf.Clamp(moveAmount, -1f, 1f);
        }
        
        public float GetSpeed() => carRb == null ? 0f : carRb.linearVelocity.magnitude;
        
        private void CapJumpHeight()
        {
            if (carRb == null) return;
            Vector3 velocity = carRb.linearVelocity;
            if (velocity.y > MAX_JUMP_HEIGHT_VELOCITY * jumpHeightCapMultiplier)
            {
                velocity.y           = MAX_JUMP_HEIGHT_VELOCITY * jumpHeightCapMultiplier;
                carRb.linearVelocity = velocity;
            }
        }
        
        private void CapMaxSpeed()
        {
            if (carRb == null || carStats == null) return;
            if (IsExceedingMaxSpeed())
                carRb.linearVelocity = carRb.linearVelocity.normalized * (carStats.MaxSpeed * speedMultiplier);
        }
        
        private bool IsExceedingMaxSpeed() =>
            carRb.linearVelocity.magnitude > (carStats.MaxSpeed * speedMultiplier);

        // ═══════════════════════════════════════════════
        //  POWER-UPS
        // ═══════════════════════════════════════════════
        
        public void ApplySpeedMultiplier(float speedMult, float accelMult)
        {
            hasZoomies             = true;
            speedMultiplier        = speedMult;
            accelerationMultiplier = accelMult;
            if (zoomiesParticles != null) zoomiesParticles.Play();
        }
        
        public void RemoveSpeedMultiplier()
        {
            hasZoomies             = false;
            speedMultiplier        = 1f;
            accelerationMultiplier = 1f;
            if (zoomiesParticles != null) zoomiesParticles.Stop();
        }

        public void ApplyJumpMultiplier(float jumpMult, float jumpHeightCapMult)
        {
            hasSuperJump            = true;
            jumpMultiplier          = jumpMult;
            jumpHeightCapMultiplier = jumpHeightCapMult;
        }
    
        public void RemoveJumpMultiplier()
        {
            hasSuperJump            = false;
            jumpMultiplier          = 1f;
            jumpHeightCapMultiplier = 1f;
        }
            
        public void TriggerSlip(float duration, float spinForce)
        {
            if (isSlipping) return;
            if (slipCoroutine != null) StopCoroutine(slipCoroutine);
            slipCoroutine = StartCoroutine(SlipRoutine(duration, spinForce));
        }
        
        private IEnumerator SlipRoutine(float duration, float spinForce)
        {
            isSlipping = true;
            SpinPlayer(spinForce);
            yield return new WaitForSeconds(duration);
            isSlipping = false;
        }

        private void SpinPlayer(float spinForce)
        {
            float   randomDirection = Random.value > 0.5f ? 1f : -1f;
            Vector3 spinTorque      = Vector3.up * spinForce * randomDirection;
            carRb.AddTorque(spinTorque, ForceMode.Impulse);
        }
    }
}