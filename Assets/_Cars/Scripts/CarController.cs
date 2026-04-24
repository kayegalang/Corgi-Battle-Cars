using System;
using System.Collections;
using _Audio.scripts;
using _Cars.ScriptableObjects;
using _Effects.Scripts;
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

        [Header("Drift Settings")]
        [SerializeField] private float driftTurnMultiplier  = 3f;
        [SerializeField] private float driftSpeedBoost      = 8f;
        [SerializeField] private float minDriftSpeedBoost   = 0.3f;

        [Header("Crash Settings")]
        [Tooltip("Minimum damage dealt at the slowest qualifying crash speed")]
        [SerializeField] private int   crashDamageMin       = 3;
        [Tooltip("Maximum damage dealt at full speed")]
        [SerializeField] private int   crashDamageMax       = 20;
        [Tooltip("Speed at which minimum crash damage is dealt")]
        [SerializeField] private float crashMinSpeed        = 3f;
        [Tooltip("Speed at which maximum crash damage is dealt")]
        [SerializeField] private float crashMaxSpeed           = 25f;
        [SerializeField] private float crashCooldown           = 0.5f;
        [Tooltip("Damage multiplier when Zoomies is active — makes ramming nearly one-shot at full speed")]
        [SerializeField] private float zoomiesCrashMultiplier  = 5f;

        [Header("Bounce Settings")]
        [SerializeField] private float bounceForce          = 8f;
        [SerializeField] private float bounceUpForce        = 2f;
        [SerializeField] private float bounceSpeedThreshold = 3f;
        
        private PlayerInput playerInput;
        private InputAction moveAction;
        private InputAction jumpAction;
        private InputAction pauseAction;
        private InputAction driftAction;
        
        private Vector2 moveInput;
        private Rigidbody carRb;
        private PauseController pauseController;
        
        public event Action OnJump;
        public event Action<float> OnLand;

        private bool    wasGrounded  = false;
        private Vector3 lastVelocity;

        private bool  hasZoomies             = false;
        private float speedMultiplier        = 1f;
        private float accelerationMultiplier = 1f;
        
        private bool  hasSuperJump            = false;
        private float jumpMultiplier          = 1f;
        private float jumpHeightCapMultiplier = 1f;
        
        private bool      isSlipping    = false;
        private Coroutine slipCoroutine;

        private bool  isDrifting = false;
        private float driftTimer = 0f;

        private float lastCrashTime = -999f;

        private DriftEffects driftEffects;
        
        private const float GROUNDED_ANGULAR_DAMPING = 3f;
        private const float AIRBORNE_ANGULAR_DAMPING = 5f;
        private const float AIRBORNE_ROTATION_SPEED  = 2f;
        private const float MAX_JUMP_HEIGHT_VELOCITY = 6f;
        private const float AIR_CONTROL_FACTOR       = 0.3f;

        // ═══════════════════════════════════════════════
        //  UNITY LIFECYCLE
        // ═══════════════════════════════════════════════
        
        private void Awake()
        {
            InitializeComponents();
            driftEffects = GetComponent<DriftEffects>();
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
            if (!CanMove()) return;

            if (isSlipping)
            {
                ApplyMovementLimits();
                lastVelocity = carRb.linearVelocity;
                return;
            }

            UpdateDriftState();
            ApplyPhysics();
            ApplyMovementLimits();
            lastVelocity = carRb.linearVelocity;
        }

        // ═══════════════════════════════════════════════
        //  CRASH
        // ═══════════════════════════════════════════════

        private void OnCollisionEnter(Collision collision)
        {
            if (!CanMove()) return;
            if (Time.time - lastCrashTime < crashCooldown) return;

            Vector3 impactDirection = collision.GetContact(0).normal;
            if (Mathf.Abs(impactDirection.y) > 0.5f) return;

            float impactSpeed = collision.relativeVelocity.magnitude;
            if (impactSpeed < crashMinSpeed) return;

            lastCrashTime = Time.time;

            if (impactSpeed >= bounceSpeedThreshold)
            {
                Vector3 bounceDir = impactDirection + Vector3.up * bounceUpForce;
                float   strength  = Mathf.Clamp01(impactSpeed / 20f) * bounceForce;
                carRb.AddForce(bounceDir.normalized * strength, ForceMode.Impulse);

                GetComponent<CameraShaker>()?.ShakeCrash(impactSpeed);
                GetComponent<ControllerRumbler>()?.RumbleCrash(impactSpeed);
            }

            int damage = CalculateCrashDamage(impactSpeed);

            // Self damage — skipped if zoomies active (crash immunity!)
            if (!hasZoomies)
            {
                CarHealth myHealth = GetComponent<CarHealth>();
                myHealth?.TakeCrashDamage(damage, null);
            }
            else
            {
                Debug.Log($"[CarController] {gameObject.name} has Zoomies — crash self-damage blocked! 🚀");
            }

            // Damage to whoever we hit — always applied, scaled by speed
            // TakeCrashDamage awards +200 to crasher and -100 to victim on death
            CarHealth theirHealth = collision.gameObject.GetComponent<CarHealth>();
            if (theirHealth != null)
                theirHealth.TakeCrashDamage(damage, gameObject);
        }

        private int CalculateCrashDamage(float impactSpeed)
        {
            float t      = Mathf.InverseLerp(crashMinSpeed, crashMaxSpeed, impactSpeed);
            int   damage = Mathf.RoundToInt(Mathf.Lerp(crashDamageMin, crashDamageMax, t));

            // Zoomies = rocket league mode — massive damage multiplier at speed
            if (hasZoomies)
                damage = Mathf.RoundToInt(damage * zoomiesCrashMultiplier);

            Debug.Log($"[CarController] Crash damage: {damage} (speed: {impactSpeed:F1}, zoomies: {hasZoomies})");
            return damage;
        }

        // ═══════════════════════════════════════════════
        //  DRIFT STATE
        // ═══════════════════════════════════════════════

        private void UpdateDriftState()
        {
            bool driftHeld = driftAction != null && driftAction.IsPressed();

            float forwardSpeed = transform.InverseTransformDirection(carRb.linearVelocity).z;
            if (driftHeld && IsGrounded() && forwardSpeed > 1f)
            {
                if (!isDrifting)
                    isDrifting = true;

                driftTimer += Time.fixedDeltaTime;
            }
            else if (isDrifting)
            {
                if (driftTimer >= minDriftSpeedBoost)
                    ApplyDriftBoost();

                isDrifting = false;
                driftTimer = 0f;
            }

            driftEffects?.SetDrifting(isDrifting, moveInput.x);
        }

        private void ApplyDriftBoost()
        {
            Vector3 boostDir = transform.forward * (moveInput.y >= 0 ? 1f : -1f);
            carRb.AddForce(boostDir * driftSpeedBoost, ForceMode.Impulse);
        }

        // ═══════════════════════════════════════════════
        //  CAN MOVE
        // ═══════════════════════════════════════════════
        
        private bool CanMove()
        {
            if (GameFlowController.instance != null && !GameFlowController.instance.IsGameplayActive())
                return false;
            return true;
        }

        // ═══════════════════════════════════════════════
        //  INITIALIZATION
        // ═══════════════════════════════════════════════
        
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
                moveAction  = actions.FindAction("Move",  true);
                jumpAction  = actions.FindAction("Jump",  true);
                pauseAction = actions.FindAction("Pause", true);
                driftAction = actions.FindAction("Drift", true);
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
                Debug.LogError($"[{nameof(CarController)}] PlayerInput not found on {gameObject.name}!");
            
            if (carRb == null)
                Debug.LogError($"[{nameof(CarController)}] Rigidbody not found on {gameObject.name}!");
            
            if (carStats == null)
                Debug.LogError($"[{nameof(CarController)}] CarStats not assigned on {gameObject.name}!");

            if (driftAction == null)
                Debug.LogWarning($"[{nameof(CarController)}] Drift action not found — add 'Drift' to PlayerControls asset!");
        }

        // ═══════════════════════════════════════════════
        //  PAUSE
        // ═══════════════════════════════════════════════
        
        private void HandlePauseInput()
        {
            if (!CanPause()) return;
            if (pauseAction.triggered) TogglePause();
        }
        
        private bool CanPause()
        {
            if (GameplayManager.instance != null && GameplayManager.instance.IsGameEnded())
                return false;
            if (GameFlowController.instance != null && !GameFlowController.instance.IsGameplayActive())
                return false;
            return true;
        }
        
        private void TogglePause()
        {
            if (pauseController == null) return;
            
            if (pauseController.GetIsPaused())
                pauseController.UnpauseGame();
            else
                pauseController.PauseGame();
        }

        // ═══════════════════════════════════════════════
        //  INPUT
        // ═══════════════════════════════════════════════
        
        private void EnableInputActions()
        {
            moveAction?.Enable();
            jumpAction?.Enable();
            pauseAction?.Enable();
            driftAction?.Enable();
        }
        
        private void DisableInputActions()
        {
            moveAction?.Disable();
            jumpAction?.Disable();
            pauseAction?.Disable();
            driftAction?.Disable();
        }
        
        private void SubscribeToInputEvents()
        {
            if (moveAction != null)
            {
                moveAction.performed += OnMovePerformed;
                moveAction.canceled  += OnMoveCanceled;
            }
            
            if (jumpAction != null)
                jumpAction.performed += OnJumpPerformed;
        }
        
        private void UnsubscribeFromInputEvents()
        {
            if (moveAction != null)
            {
                moveAction.performed -= OnMovePerformed;
                moveAction.canceled  -= OnMoveCanceled;
            }
            
            if (jumpAction != null)
                jumpAction.performed -= OnJumpPerformed;
        }
        
        private void OnMovePerformed(InputAction.CallbackContext ctx) => moveInput = ctx.ReadValue<Vector2>();
        private void OnMoveCanceled(InputAction.CallbackContext ctx)  => moveInput = Vector2.zero;
        private void OnJumpPerformed(InputAction.CallbackContext ctx) => Jump();

        // ═══════════════════════════════════════════════
        //  CINEMATIC MODE
        // ═══════════════════════════════════════════════

        public void SetGamepadOnly(bool gamepadOnly)
        {
            if (playerInput == null) return;

            if (gamepadOnly)
            {
                var gamepad = Gamepad.current;
                if (gamepad != null)
                    playerInput.SwitchCurrentControlScheme("Controller", gamepad);
                else
                    Debug.LogWarning($"[CarController] SetGamepadOnly: no gamepad found!");
            }
            else
            {
                var keyboard = Keyboard.current;
                var mouse    = Mouse.current;
                if (keyboard != null && mouse != null)
                    playerInput.SwitchCurrentControlScheme("Keyboard", keyboard, mouse);
            }
        }

        // ═══════════════════════════════════════════════
        //  PHYSICS
        // ═══════════════════════════════════════════════
        
        private void ApplyPhysics()
        {
            Move();
            Turn();
            ApplyAngularDamping();
            LevelRotationInAir();
            CheckLanding();
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
                if (!isDrifting) ConstrainLateralMovement();
                return;
            }
    
            ApplyAcceleration();

            if (isDrifting)
                ApplyDriftLateralDamping();
            else
                ConstrainLateralMovement();
        }

        private void ApplyDriftLateralDamping()
        {
            Vector3 localVelocity = transform.InverseTransformDirection(carRb.linearVelocity);
            localVelocity.x *= 0.75f;
            carRb.linearVelocity = transform.TransformDirection(localVelocity);
        }
        
        private bool ShouldMove() => moveInput.y != 0;
        
        private void ApplyAcceleration()
        {
            Vector3 direction = moveInput.y > 0 ? Vector3.forward : -Vector3.forward;
            Vector3 force     = direction * carStats.Acceleration * accelerationMultiplier;
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
            if (!ShouldTurn()) return;

            float turnDirection = IsMovingForward() ? moveInput.x : -moveInput.x;
            float controlFactor = IsGrounded() ? 1f : AIR_CONTROL_FACTOR;
            float driftFactor   = isDrifting ? driftTurnMultiplier : 1f;

            Vector3 torque = Vector3.up * turnDirection * carStats.TurnSpeed * controlFactor * driftFactor;
            carRb.AddTorque(torque);
        }
        
        private bool ShouldTurn()      => moveInput.x != 0;
        private bool IsMovingForward() => moveInput.y >= 0;
        
        private void ApplyAngularDamping()
        {
            carRb.angularDamping = IsGrounded() ? GROUNDED_ANGULAR_DAMPING : AIRBORNE_ANGULAR_DAMPING;
        }

        private void CheckLanding()
        {
            bool grounded = IsGrounded();
            if (grounded && !wasGrounded)
            {
                float landingSpeed = Mathf.Abs(lastVelocity.y);
                GetComponent<CameraShaker>()?.ShakeLand();
                GetComponent<ControllerRumbler>()?.RumbleLand();
                OnLand?.Invoke(landingSpeed);
            }
            wasGrounded = grounded;
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
        
        private void Jump()
        {
            if (!CanJump()) return;
            Vector3 jumpForce = transform.up * carStats.JumpForce * jumpMultiplier;
            carRb.AddForce(jumpForce, ForceMode.Impulse);
            OnJump?.Invoke();

            // Play jump or super jump sound
            if (AudioManager.instance != null && FMODEvents.instance != null)
            {
                var sound = hasSuperJump
                    ? FMODEvents.instance.superjump
                    : FMODEvents.instance.jump;
                AudioManager.instance.PlayOneShot(sound, transform.position);
            }
        }
        
        private bool CanJump() => IsGrounded() && carStats != null;
        
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
        //  ZOOMIES POWER-UP
        // ═══════════════════════════════════════════════
        
        public void ApplySpeedMultiplier(float speedMult, float accelMult)
        {
            hasZoomies             = true;
            speedMultiplier        = speedMult;
            accelerationMultiplier = accelMult;
    
            if (zoomiesParticles != null)
                zoomiesParticles.Play();

            GetComponent<ControllerRumbler>()?.RumbleZoomiesStart();

            // Play zoomies sound
            if (AudioManager.instance != null && FMODEvents.instance != null)
                AudioManager.instance.PlayOneShot(FMODEvents.instance.zoomies, transform.position);
        }

        

        /// <summary>Called by CarVisualLoader after spawning the car prefab.</summary>
        public void SetZoomiesParticles(ParticleSystem particles)
        {
            zoomiesParticles = particles;
            Debug.Log("[CarController] ZoomiesParticles rewired from spawned car prefab.");
        }
        
        public void RemoveSpeedMultiplier()
        {
            hasZoomies             = false;
            speedMultiplier        = 1f;
            accelerationMultiplier = 1f;
    
            if (zoomiesParticles != null)
                zoomiesParticles.Stop();

            GetComponent<ControllerRumbler>()?.RumbleZoomiesStop();

            // Stop zoomies sound
            if (AudioManager.instance != null && FMODEvents.instance != null)
                AudioManager.instance.PlayOneShot(FMODEvents.instance.zoomies, transform.position);
        }

        // ═══════════════════════════════════════════════
        //  SUPER JUMP POWER-UP
        // ═══════════════════════════════════════════════
        
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

        // ═══════════════════════════════════════════════
        //  POOP POWER-UP
        // ═══════════════════════════════════════════════
        
        public void TriggerSlip(float duration, float spinForce)
        {
            if (isSlipping) return;

            GetComponent<CameraShaker>()?.ShakePoopSlip();
            GetComponent<ControllerRumbler>()?.RumblePoopSlip();
            
            if (carRb == null)
            {
                Debug.LogError($"[CarController] Cannot trigger slip - Rigidbody is null on {gameObject.name}!");
                return;
            }
            
            if (slipCoroutine != null)
                StopCoroutine(slipCoroutine);
            
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
            if (carRb == null) return;
            float   randomDirection = UnityEngine.Random.value > 0.5f ? 1f : -1f;
            Vector3 spinTorque      = Vector3.up * (spinForce * randomDirection);
            carRb.AddTorque(spinTorque, ForceMode.Impulse);
        }

        // ═══════════════════════════════════════════════
        //  SQUIRREL POWER-UP
        // ═══════════════════════════════════════════════

        public void SetInputs(float turnAmount, float moveAmount)
        {
            moveInput.x = Mathf.Clamp(turnAmount, -1f, 1f);
            moveInput.y = Mathf.Clamp(moveAmount, -1f, 1f);
        }

        public Vector2 GetMoveInput() => moveInput;
    }
}