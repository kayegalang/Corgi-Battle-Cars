using _Projectiles.Scripts;
using _Projectiles.ScriptableObjects;
using _Gameplay.Scripts;
using _UI.Scripts;
using UnityEngine;
using UnityEngine.InputSystem;

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
        
        [SerializeField] [Tooltip("How fast the reticle moves with mouse")]
        [Range(0.1f, 5f)]
        private float mouseSensitivity = 1.5f;

        private CooldownBarUI cooldownBar;

        private PlayerInput playerInput;
        private InputAction shootAction;
        private InputAction aimAction;
        private Rigidbody carRigidbody;
        private PauseController pauseController;
        private Camera playerCamera; 
        private Transform firePoint;
        
        private Vector2 controllerAimInput;
        private Vector2 lastMousePosition;
        private bool usingMouseForAiming = true;
        private bool isFiring;
        
        private float currentCooldown;
        private bool weaponIsReady = true;

        private void Awake()
        {
            CacheComponents();
            BindInputActions();
            InitializeReticleState();
            CaptureInitialMousePosition();
            ValidateProjectileType();
            FindCooldownBar();
        }

        private void CacheComponents()
        {
            playerInput = GetComponent<PlayerInput>();
            carRigidbody = GetComponent<Rigidbody>();
            firePoint = transform.Find("FirePoint");
            playerCamera = GetComponentInChildren<Camera>();
            pauseController = FindFirstObjectByType<PauseController>();
        }

        private void BindInputActions()
        {
            var actions = playerInput.actions;
            shootAction = actions.FindAction("Shoot", throwIfNotFound: true);
            aimAction = actions.FindAction("Aim", throwIfNotFound: true);
        }

        private void InitializeReticleState()
        {
            HideReticle();
        }

        private void CaptureInitialMousePosition()
        {
            if (Mouse.current != null)
            {
                lastMousePosition = Mouse.current.position.ReadValue();
            }
        }

        private void ValidateProjectileType()
        {
            if (projectileType == null)
            {
                Debug.LogError($"[{nameof(CarShooter)}] No projectile type assigned on {gameObject.name}!");
            }
        }

        private void FindCooldownBar()
        {
            if (cooldownBar != null)
            {
                Debug.Log($"[{nameof(CarShooter)}] Cooldown bar already assigned for {gameObject.name}");
                return;
            }

            if (playerCamera == null)
            {
                Debug.LogWarning($"[{nameof(CarShooter)}] Cannot find cooldown bar - no camera found on {gameObject.name}");
                return;
            }

            cooldownBar = playerCamera.GetComponentInChildren<CooldownBarUI>();

            if (cooldownBar != null)
            {
                Debug.Log($"[{nameof(CarShooter)}] ✓ Found cooldown bar for {gameObject.name}: {cooldownBar.gameObject.name}");
            }
            else
            {
                Debug.LogWarning($"[{nameof(CarShooter)}] No CooldownBarUI found in camera hierarchy for {gameObject.name}! Cooldown bar will not display.");
            }
        }

        private void OnEnable()
        {
            EnableInputActions();
            SubscribeToInputEvents();
        }

        private void EnableInputActions()
        {
            shootAction.Enable();
            aimAction.Enable();
        }

        private void SubscribeToInputEvents()
        {
            shootAction.performed += StartFiring;
            shootAction.canceled += StopFiring;
            aimAction.performed += UpdateControllerAim;
            aimAction.canceled += ResetControllerAim;
        }

        private void OnDisable()
        {
            DisableInputActions();
            UnsubscribeFromInputEvents();
            CleanupUIState();
        }

        private void DisableInputActions()
        {
            shootAction.Disable();
            aimAction.Disable();
        }

        private void UnsubscribeFromInputEvents()
        {
            shootAction.performed -= StartFiring;
            shootAction.canceled -= StopFiring;
            aimAction.performed -= UpdateControllerAim;
            aimAction.canceled -= ResetControllerAim;
        }

        private void CleanupUIState()
        {
            HideReticle();
            
            // Only restore cursor visibility for keyboard/mouse players.
            // A gamepad player never owns the cursor, so touching it here
            // would fight a dead keyboard player who needs it visible.
            if (IsKeyboardPlayer())
            {
                ShowAndUnlockCursor();
            }
        }

        private bool IsKeyboardPlayer()
        {
            return playerInput != null && playerInput.currentControlScheme == "Keyboard";
        }

        private void ShowAndUnlockCursor()
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }

        private void StartFiring(InputAction.CallbackContext context)
        {
            isFiring = true;
        }
        
        private void StopFiring(InputAction.CallbackContext context)
        {
            isFiring = false;
        }
        
        private void UpdateControllerAim(InputAction.CallbackContext context)
        {
            controllerAimInput = context.ReadValue<Vector2>();
            
            if (ControllerStickIsBeingUsed())
            {
                SwitchToControllerAiming();
            }
        }

        private bool ControllerStickIsBeingUsed()
        {
            return controllerAimInput.sqrMagnitude > 0.1f;
        }

        private void SwitchToControllerAiming()
        {
            usingMouseForAiming = false;
        }
        
        private void ResetControllerAim(InputAction.CallbackContext context)
        {
            controllerAimInput = Vector2.zero;
        }

        private void Update()
        {
            UpdateCooldownProgress();
            UpdateCooldownBar();
            
            if (GameHasEnded())
            {
                ShowCursorAndHideReticle();
                return;
            }
            
            if (GameIsPaused())
            {
                ShowCursorAndHideReticle();
            }
            else
            {
                HideCursorAndShowReticle();
                UpdateReticlePosition();
            }
        }

        private bool GameplayIsDisabled()
        {
            return !gameplayEnabled;
        }

        private bool GameHasEnded()
        {
            return GameplayManager.instance != null && GameplayManager.instance.IsGameEnded();
        }

        private bool GameIsPaused()
        {
            return pauseController != null && pauseController.GetIsPaused();
        }

        private void UpdateCooldownProgress()
        {
            if (weaponIsReady)
            {
                return;
            }
            
            currentCooldown += Time.deltaTime;
            
            if (CooldownCompleted())
            {
                SetWeaponReady();
            }
        }

        private bool CooldownCompleted()
        {
            if (projectileType == null)
            {
                return true;
            }
            
            return currentCooldown >= projectileType.CooldownDuration;
        }

        private void SetWeaponReady()
        {
            weaponIsReady = true;
            currentCooldown = projectileType != null ? projectileType.CooldownDuration : 0f;
        }

        private void UpdateCooldownBar()
        {
            if (cooldownBar == null || projectileType == null)
            {
                return;
            }
            
            cooldownBar.UpdateCooldown(currentCooldown, projectileType.CooldownDuration);
        }

        private void FixedUpdate()
        {
            if (GameplayIsDisabled())
            {
                return;
            }
            
            if (PlayerPressingFireButton() && CanShootNow())
            {
                FireProjectile();
            }
        }

        private bool PlayerPressingFireButton()
        {
            return isFiring;
        }

        private bool CanShootNow()
        {
            return weaponIsReady && projectileType != null;
        }

        private void UpdateReticlePosition()
        {
            DetectMouseMovementAndSwitch();
            
            if (usingMouseForAiming)
            {
                MoveReticleWithMouseDelta();
            }
            else
            {
                MoveReticleWithControllerStick();
            }
        }

        private void DetectMouseMovementAndSwitch()
        {
            if (Mouse.current == null)
            {
                return;
            }
            
            Vector2 currentMousePosition = Mouse.current.position.ReadValue();
            Vector2 mouseDelta = currentMousePosition - lastMousePosition;
            
            if (mouseDelta.sqrMagnitude > 0.1f)
            {
                SwitchToMouseAiming();
            }
        }

        private void SwitchToMouseAiming()
        {
            usingMouseForAiming = true;
        }

        private void MoveReticleWithMouseDelta()
        {
            if (Mouse.current == null)
            {
                return;
            }
            
            Vector2 currentMousePosition = Mouse.current.position.ReadValue();
            Vector2 mouseDelta = currentMousePosition - lastMousePosition;
            lastMousePosition = currentMousePosition;
            
            reticle.anchoredPosition += mouseDelta * mouseSensitivity;
            reticle.anchoredPosition = ClampPositionToCanvas();
        }

        private void MoveReticleWithControllerStick()
        {
            reticle.anchoredPosition += controllerAimInput * controllerSensitivity * Time.deltaTime;
            reticle.anchoredPosition = ClampPositionToCanvas();
        }

        private Vector2 ClampPositionToCanvas()
        {
            RectTransform canvasRect = reticleCanvas.transform as RectTransform;
            Rect viewportRect = playerCamera.pixelRect;
            
            Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(null, reticle.position);
            
            screenPos.x = Mathf.Clamp(screenPos.x, viewportRect.xMin, viewportRect.xMax);
            screenPos.y = Mathf.Clamp(screenPos.y, viewportRect.yMin, viewportRect.yMax);
            
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect,
                screenPos,
                null,
                out Vector2 localPoint
            );
            
            return localPoint;
        }
        
        private void FireProjectile()
        {
            Vector3 shootDirection = CalculateShootDirectionFromReticle();
            GameObject projectile = CreateProjectileAtFirePoint(shootDirection);
            
            ConfigureProjectileStats(projectile);
            ApplyVelocityAndForceToProjectile(projectile, shootDirection);
            ApplyRecoilToShooter(shootDirection);
            StartCooldown();
        }

        private Vector3 CalculateShootDirectionFromReticle()
        {
            Vector2 reticleScreenPosition = RectTransformUtility.WorldToScreenPoint(null, reticle.position);
            Ray rayFromReticle = playerCamera.ScreenPointToRay(reticleScreenPosition);

            if (RayHitsSomething(rayFromReticle, out RaycastHit hit))
            {
                return CalculateDirectionToHitPoint(hit.point);
            }

            return CalculateDirectionToDefaultDistance(rayFromReticle);
        }

        private bool RayHitsSomething(Ray ray, out RaycastHit hit)
        {
            return Physics.Raycast(ray, out hit);
        }

        private Vector3 CalculateDirectionToHitPoint(Vector3 hitPoint)
        {
            return (hitPoint - firePoint.position).normalized;
        }

        private Vector3 CalculateDirectionToDefaultDistance(Ray ray)
        {
            const float DEFAULT_DISTANCE = 50f;
            return (ray.GetPoint(DEFAULT_DISTANCE) - firePoint.position).normalized;
        }

        private GameObject CreateProjectileAtFirePoint(Vector3 direction)
        {
            Quaternion rotation = Quaternion.LookRotation(direction);
            return Instantiate(projectileType.ProjectilePrefab, firePoint.position, rotation);
        }

        private void ConfigureProjectileStats(GameObject projectile)
        {
            Projectile projectileComponent = projectile.GetComponent<Projectile>();
            
            if (projectileComponent != null)
            {
                projectileComponent.ConfigureProjectile(
                    gameObject,
                    projectileType.Damage,
                    projectileType.Lifetime
                );
            }
        }

        private void ApplyVelocityAndForceToProjectile(GameObject projectile, Vector3 direction)
        {
            Rigidbody projectileRigidbody = projectile.GetComponent<Rigidbody>();
            
            if (projectileRigidbody != null)
            {
                InheritCarVelocity(projectileRigidbody);
                ApplyShootingForce(projectileRigidbody, direction);
            }
        }

        private void InheritCarVelocity(Rigidbody projectileRigidbody)
        {
            projectileRigidbody.linearVelocity = carRigidbody.linearVelocity;
        }

        private void ApplyShootingForce(Rigidbody projectileRigidbody, Vector3 direction)
        {
            projectileRigidbody.AddForce(direction * projectileType.FireForce, ForceMode.Impulse);
        }

        private void ApplyRecoilToShooter(Vector3 shootDirection)
        {
            if (projectileType.RecoilForce <= 0f)
            {
                return;
            }
            
            Vector3 recoilDirection = -shootDirection;
            Vector3 recoilForce = recoilDirection * projectileType.RecoilForce;
            
            carRigidbody.AddForce(recoilForce, ForceMode.Impulse);
        }

        private void StartCooldown()
        {
            weaponIsReady = false;
            currentCooldown = 0f;
        }
        
        public void EnableGameplay()
        {
            gameplayEnabled = true;
            CenterReticleInViewport();
            
            // Reset lastMousePosition to current mouse position so MoveReticleWithMouse()
            // doesn't immediately apply a delta from before death/respawn.
            if (Mouse.current != null)
            {
                lastMousePosition = Mouse.current.position.ReadValue();
            }
        }

        private int GetPlayerCount()
        {
            return UnityEngine.InputSystem.PlayerInput.all.Count;
        }
        
        private void CenterReticleInViewport()
        {
            if (reticle == null || playerCamera == null || reticleCanvas == null)
            {
                Debug.LogWarning($"[CarShooter] Cannot center reticle - missing components");
                return;
            }
            
            int playerCount = GetPlayerCount();
            
            // Parse player number from tag (e.g. "PlayerOne" → 1, "PlayerTwo" → 2)
            string tag = gameObject.tag;
            int playerNumber = 1; // default
            if (tag.Contains("One")) playerNumber = 1;
            else if (tag.Contains("Two")) playerNumber = 2;
            else if (tag.Contains("Three")) playerNumber = 3;
            else if (tag.Contains("Four")) playerNumber = 4;
            
            Vector2 targetPosition = Vector2.zero;

            if (playerCount == 1)
            {
                // Fullscreen - center at (0.5f, 0.5f)
                targetPosition = new Vector2(0.5f, 0.5f);
            }
            else if (playerCount == 2)
            {
                // Vertical split - left/right
                targetPosition = playerNumber == 1 ? new Vector2(-480, 0) : new Vector2(480, 0);
            }
            else if (playerCount == 3 || playerCount == 4)
            {
                // 2x2 grid
                if (playerNumber == 1)
                    targetPosition = new Vector2(-480, 270);   // top left
                else if (playerNumber == 2)
                    targetPosition = new Vector2(480, 270);    // top right
                else if (playerNumber == 3)
                    targetPosition = new Vector2(-480, -270);  // bottom left
                else
                    targetPosition = new Vector2(480, -270);   // bottom right
            }

            reticle.anchoredPosition = targetPosition;

            Debug.Log($"[{gameObject.name}] playerCount={playerCount} playerNum={playerNumber} anchoredPosition={targetPosition}");
        }
        
        public void DisableGameplay()
        {
            gameplayEnabled = false;
            ShowCursorAndHideReticle();
        }

        public void DisableReticle()
        {
            ShowCursorAndHideReticle();
        }
        
        private void ShowCursorAndHideReticle()
        {
            SetCursorVisibility(visible: true);
        }

        private void HideCursorAndShowReticle()
        {
            SetCursorVisibility(visible: false);
        }
        
        private void SetCursorVisibility(bool visible)
        {
            if (reticle != null)
            {
                reticle.gameObject.SetActive(!visible);
            }
            
            if (!IsKeyboardPlayer())
            {
                return;
            }

            Cursor.visible = visible;
            Cursor.lockState = visible ? CursorLockMode.None : CursorLockMode.Locked;
        }

        private void HideReticle()
        {
            if (reticle != null)
            {
                reticle.gameObject.SetActive(false);
            }
        }
    }
}