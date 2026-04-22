using _Projectiles.Scripts;
using _Projectiles.ScriptableObjects;
using _Gameplay.Scripts;
using _UI.Scripts;
using _Effects.Scripts;
using UnityEngine;
using UnityEngine.InputSystem;
using _Audio.scripts;


namespace _Cars.Scripts
{
    public class CarShooter : MonoBehaviour
    {
        [Header("Gameplay State")]
        [SerializeField] [Tooltip("Whether shooting is currently enabled")]
        private bool gameplayEnabled;

        [Header("Projectile Configuration")]
        [SerializeField] [Tooltip("The projectile type to shoot")]
        private ProjectileObject projectileType;

        [Header("Reticle Configuration")]
        [SerializeField] [Tooltip("The UI element that shows where you're aiming")]
        private RectTransform reticle;
        
        [SerializeField] [Tooltip("Canvas containing the reticle")]
        private Canvas reticleCanvas;
        
        [SerializeField] [Tooltip("How fast the reticle moves with controller stick")]
        [Range(100f, 2000f)]
        private float controllerSensitivity = 900f;

        [SerializeField] [Tooltip("The transform we fire projectiles from")]
        private Transform firePoint;
        
        [SerializeField] [Tooltip("How fast the reticle moves with mouse")]
        [Range(0.1f, 5f)]
        private float mouseSensitivity = 1.5f;
        [SerializeField] private CooldownBarUI cooldownBar;

        private PlayerInput     playerInput;
        private InputAction     shootAction;
        private InputAction     aimAction;
        private Rigidbody       carRigidbody;
        private PauseController pauseController;
        private Camera          playerCamera;
        
        private Vector2 controllerAimInput;
        private Vector2 lastMousePosition;
        private bool    usingMouseForAiming = true;
        private bool    isFiring;
        
        // Charge system
        private float currentCharge;
        private const float MAX_CHARGE      = 100f;
        private const float CHARGE_PER_SHOT = 10f;
        private float nextAllowedFireTime;
        private bool  isOverheated = false;

        private float FireRate        => projectileType != null ? projectileType.FireRate        : 0.5f;
        private float ChargeRegenRate => projectileType != null ? MAX_CHARGE / Mathf.Max(projectileType.CooldownDuration, 0.1f) : 10f;

        // ═══════════════════════════════════════════════
        //  LIFECYCLE
        // ═══════════════════════════════════════════════

        private void Awake()
        {
            CacheComponents();
            BindInputActions();
            InitializeReticleState();
            CaptureInitialMousePosition();
            ValidateProjectileType();
        }

        private void CacheComponents()
        {
            playerInput     = GetComponent<PlayerInput>();
            carRigidbody    = GetComponent<Rigidbody>();
            playerCamera    = GetComponentInChildren<Camera>();
            pauseController = FindFirstObjectByType<PauseController>();
        }

        private void BindInputActions()
        {
            var actions = playerInput.actions;
            shootAction = actions.FindAction("Shoot", throwIfNotFound: true);
            aimAction   = actions.FindAction("Aim",   throwIfNotFound: true);
        }

        private void InitializeReticleState()
        {
            HideReticle();
            currentCharge = MAX_CHARGE;
        }

        private void CaptureInitialMousePosition()
        {
            if (Mouse.current != null)
                lastMousePosition = Mouse.current.position.ReadValue();
        }

        private void ValidateProjectileType()
        {
            if (projectileType == null)
                Debug.LogError($"[{nameof(CarShooter)}] No projectile type assigned on {gameObject.name}!");
        }

        // ═══════════════════════════════════════════════
        //  ENABLE / DISABLE
        // ═══════════════════════════════════════════════

        private void OnEnable()
        {
            EnableInputActions();
            SubscribeToInputEvents();
        }
        
        private void OnDisable()
        {
            DisableInputActions();
            UnsubscribeFromInputEvents();
            CleanupUIState();
        }

        private void EnableInputActions()
        {
            shootAction.Enable();
            aimAction.Enable();
        }

        private void DisableInputActions()
        {
            shootAction.Disable();
            aimAction.Disable();
        }

        private void SubscribeToInputEvents()
        {
            shootAction.performed += StartFiring;
            shootAction.canceled  += StopFiring;
            aimAction.performed   += UpdateControllerAim;
            aimAction.canceled    += ResetControllerAim;
        }

        private void UnsubscribeFromInputEvents()
        {
            shootAction.performed -= StartFiring;
            shootAction.canceled  -= StopFiring;
            aimAction.performed   -= UpdateControllerAim;
            aimAction.canceled    -= ResetControllerAim;
        }

        private void CleanupUIState()
        {
            if (IsKeyboardPlayer())
                ShowAndUnlockCursor();
        }

        private bool IsKeyboardPlayer() =>
            playerInput != null && playerInput.currentControlScheme == "Keyboard";

        private void ShowAndUnlockCursor()
        {
            Cursor.visible   = true;
            Cursor.lockState = CursorLockMode.None;
        }

        // ═══════════════════════════════════════════════
        //  INPUT CALLBACKS
        // ═══════════════════════════════════════════════

        private void StartFiring(InputAction.CallbackContext context) => isFiring = true;
        private void StopFiring(InputAction.CallbackContext context)  => isFiring = false;

        private void UpdateControllerAim(InputAction.CallbackContext context)
        {
            controllerAimInput = context.ReadValue<Vector2>();
            if (controllerAimInput.sqrMagnitude > 0.1f)
                usingMouseForAiming = false;
        }

        private void ResetControllerAim(InputAction.CallbackContext context) =>
            controllerAimInput = Vector2.zero;

        // ═══════════════════════════════════════════════
        //  UPDATE
        // ═══════════════════════════════════════════════

        private void Update()
        {
            RegenerateCharge();
            UpdateCooldownBar();

            if (GameHasEnded() || GameplayIsDisabled())
            {
                ShowCursorAndHideReticle();
                return;
            }
    
            if (GameIsPaused())
                ShowCursorAndHideReticle();
            else
            {
                HideCursorAndShowReticle();
                UpdateReticlePosition();
            }
        }

        private void RegenerateCharge()
        {
            if (currentCharge >= MAX_CHARGE) return;

            currentCharge = Mathf.Min(currentCharge + ChargeRegenRate * Time.deltaTime, MAX_CHARGE);

            if (currentCharge >= MAX_CHARGE && isOverheated)
                isOverheated = false;
        }

        private bool GameplayIsDisabled() => !gameplayEnabled;
        private bool GameHasEnded()       => GameplayManager.instance != null && GameplayManager.instance.IsGameEnded();
        private bool GameIsPaused()       => pauseController != null && pauseController.GetIsPaused();

        private void UpdateCooldownBar()
        {
            if (cooldownBar == null) return;

            Debug.Log($"Charge: {currentCharge} / {MAX_CHARGE}");

            cooldownBar.SetCooldown(currentCharge, MAX_CHARGE);
        }

        // ═══════════════════════════════════════════════
        //  FIXED UPDATE — SHOOTING
        // ═══════════════════════════════════════════════

        private void FixedUpdate()
        {
            if (GameplayIsDisabled()) return;

            if (PlayerPressingFireButton() && CanShootNow())
            {
                bool willOverheat = (currentCharge - CHARGE_PER_SHOT) < CHARGE_PER_SHOT;

                FireProjectile();
                currentCharge -= CHARGE_PER_SHOT;

                if (willOverheat)
                {
                    currentCharge = 0f;
                    isOverheated  = true;
                }

                nextAllowedFireTime = Time.time + FireRate;
            }
        }

        private bool PlayerPressingFireButton() => isFiring;

        private bool CanShootNow()
        {
            if (projectileType == null) return false;
            if (isOverheated)           return false;
            return currentCharge >= CHARGE_PER_SHOT && Time.time >= nextAllowedFireTime;
        }

        // ═══════════════════════════════════════════════
        //  RETICLE
        // ═══════════════════════════════════════════════

        private void UpdateReticlePosition()
        {
            DetectMouseMovementAndSwitch();

            if (usingMouseForAiming)
                MoveReticleWithMouseDelta();
            else
                MoveReticleWithControllerStick();
        }

        private void DetectMouseMovementAndSwitch()
        {
            if (Mouse.current == null) return;

            Vector2 delta = Mouse.current.position.ReadValue() - lastMousePosition;
            if (delta.sqrMagnitude > 0.1f)
                usingMouseForAiming = true;
        }

        private void MoveReticleWithMouseDelta()
        {
            if (Mouse.current == null) return;

            Vector2 currentPos = Mouse.current.position.ReadValue();
            Vector2 delta      = currentPos - lastMousePosition;
            lastMousePosition  = currentPos;

            reticle.anchoredPosition += delta * mouseSensitivity;
            reticle.anchoredPosition  = ClampPositionToCanvas();
        }

        private void MoveReticleWithControllerStick()
        {
            reticle.anchoredPosition += controllerAimInput * controllerSensitivity * Time.deltaTime;
            reticle.anchoredPosition  = ClampPositionToCanvas();
        }

        private Vector2 ClampPositionToCanvas()
        {
            RectTransform canvasRect   = reticleCanvas.transform as RectTransform;
            Rect          viewportRect = playerCamera.pixelRect;

            Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(null, reticle.position);
            screenPos.x = Mathf.Clamp(screenPos.x, viewportRect.xMin, viewportRect.xMax);
            screenPos.y = Mathf.Clamp(screenPos.y, viewportRect.yMin, viewportRect.yMax);

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect, screenPos, null, out Vector2 localPoint);

            return localPoint;
        }

        // ═══════════════════════════════════════════════
        //  FIRING
        // ═══════════════════════════════════════════════

        private void FireProjectile()
        {
            Vector3    dir        = CalculateShootDirectionFromReticle();
            GameObject projectile = CreateProjectileAtFirePoint(dir);

            ConfigureProjectileStats(projectile);
            ApplyVelocityAndForceToProjectile(projectile, dir);
            ApplyRecoilToShooter(dir);

            GetComponent<CameraShaker>()?.ShakeShoot();
            GetComponent<ControllerRumbler>()?.RumbleShoot();  // ← vibration
            
            AudioManager.instance.PlayOneShot(FMODEvents.instance.shootsound, this.transform.position);
        }

        private Vector3 CalculateShootDirectionFromReticle()
        {
            Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(null, reticle.position);
            Ray     ray       = playerCamera.ScreenPointToRay(screenPos);

            if (Physics.Raycast(ray, out RaycastHit hit))
                return (hit.point - firePoint.position).normalized;

            return (ray.GetPoint(50f) - firePoint.position).normalized;
        }

        private GameObject CreateProjectileAtFirePoint(Vector3 direction) =>
            Instantiate(projectileType.ProjectilePrefab, firePoint.position, Quaternion.LookRotation(direction));

        private void ConfigureProjectileStats(GameObject projectile)
        {
            var comp = projectile.GetComponent<Projectile>();
            if (comp != null)
                comp.ConfigureProjectile(gameObject, projectileType.Damage, projectileType.Lifetime);
        }

        private void ApplyVelocityAndForceToProjectile(GameObject projectile, Vector3 direction)
        {
            Rigidbody rb = projectile.GetComponent<Rigidbody>();
            if (rb == null) return;

            rb.linearVelocity = carRigidbody.linearVelocity;
            rb.AddForce(direction * projectileType.FireForce, ForceMode.Impulse);
        }

        private void ApplyRecoilToShooter(Vector3 shootDirection)
        {
            if (projectileType.RecoilForce <= 0f) return;

            Vector3 horizontalDir  = new Vector3(shootDirection.x, 0f, shootDirection.z).normalized;
            Vector3 recoilDir      = -horizontalDir;
            Vector3 horizontalVel  = new Vector3(carRigidbody.linearVelocity.x, 0f, carRigidbody.linearVelocity.z);
            float   velAlongRecoil = Vector3.Dot(horizontalVel, recoilDir);
            float   recoilScale    = velAlongRecoil < 0f ? 0.5f : 1f;

            carRigidbody.AddForce(recoilDir * projectileType.RecoilForce * recoilScale, ForceMode.Impulse);
        }

        // ═══════════════════════════════════════════════
        //  GAMEPLAY ENABLE / DISABLE
        // ═══════════════════════════════════════════════

        public void EnableGameplay()
        {
            gameplayEnabled = true;
            CenterReticleInViewport();

            if (Mouse.current != null)
                lastMousePosition = Mouse.current.position.ReadValue();
        }

        public void DisableGameplay()
        {
            gameplayEnabled = false;
            ShowCursorAndHideReticle();
        }

        public void DisableReticle() => ShowCursorAndHideReticle();

        private void ShowCursorAndHideReticle() => SetCursorVisibility(visible: true);
        private void HideCursorAndShowReticle()  => SetCursorVisibility(visible: false);

        private void SetCursorVisibility(bool visible)
        {
            if (reticle != null)
                reticle.gameObject.SetActive(!visible);

            if (!IsKeyboardPlayer()) return;

            Cursor.visible   = visible;
            Cursor.lockState = visible ? CursorLockMode.None : CursorLockMode.Locked;
        }

        private void HideReticle()
        {
            if (reticle != null)
                reticle.gameObject.SetActive(false);
        }

        private int GetPlayerCount() => UnityEngine.InputSystem.PlayerInput.all.Count;

        private void CenterReticleInViewport()
        {
            if (reticle == null || playerCamera == null || reticleCanvas == null)
            {
                Debug.LogWarning($"[CarShooter] Cannot center reticle - missing components");
                return;
            }

            int playerCount  = GetPlayerCount();
            int playerNumber = 1;
            string tag       = gameObject.tag;

            if      (tag.Contains("One"))   playerNumber = 1;
            else if (tag.Contains("Two"))   playerNumber = 2;
            else if (tag.Contains("Three")) playerNumber = 3;
            else if (tag.Contains("Four"))  playerNumber = 4;

            Vector2 targetPosition = Vector2.zero;

            if (playerCount == 1)
                targetPosition = new Vector2(0.5f, 0.5f);
            else if (playerCount == 2)
                targetPosition = playerNumber == 1 ? new Vector2(-480, 0) : new Vector2(480, 0);
            else if (playerCount == 3 || playerCount == 4)
            {
                if      (playerNumber == 1) targetPosition = new Vector2(-480,  270);
                else if (playerNumber == 2) targetPosition = new Vector2( 480,  270);
                else if (playerNumber == 3) targetPosition = new Vector2(-480, -270);
                else                        targetPosition = new Vector2( 480, -270);
            }

            reticle.anchoredPosition = targetPosition;
        }

        // ═══════════════════════════════════════════════
        //  SET PROJECTILE TYPE (runtime swap)
        // ═══════════════════════════════════════════════
        
        public bool IsFiring() => isFiring;
        
        public void SetProjectileType(ProjectileObject newProjectile)
        {
            if (newProjectile == null)
            {
                Debug.LogWarning($"[{nameof(CarShooter)}] Cannot set null projectile type!");
                return;
            }

            projectileType      = newProjectile;
            currentCharge       = MAX_CHARGE;
            isOverheated        = false;
            nextAllowedFireTime = 0f;
        }
        
        // ═══════════════════════════════════════════════
        //  AIM DIRECTION (used by TurretVisuals)
        // ═══════════════════════════════════════════════

        public Vector3 GetAimDirection()
        {
            if (reticle == null || playerCamera == null || firePoint == null) return Vector3.zero;
            return CalculateShootDirectionFromReticle();
        }
    }
}