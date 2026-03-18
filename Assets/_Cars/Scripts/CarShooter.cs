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
        
        // Charge system - all values driven by ProjectileObject SO
        private float currentCharge;
        private const float MAX_CHARGE = 100f;
        private const float CHARGE_PER_SHOT = 10f;  // Fixed: 10 shots before overheat
        private float chargeRegenRate;               // Derived: MAX_CHARGE / CooldownDuration
        private float fireRate;                      // From: ProjectileObject.FireRate
        private float nextAllowedFireTime;
        private bool isOverheated = false;

        private void Awake()
        {
            CacheComponents();
            BindInputActions();
            InitializeReticleState();
            CaptureInitialMousePosition();
            ValidateProjectileType();
            InitializeFromProjectile();
        }

        private void Start()
        {
            // Delay one frame to let HealthBarManager create PlayerHealthCanvas
            StartCoroutine(FindCooldownBarDelayed());
        }

        private System.Collections.IEnumerator FindCooldownBarDelayed()
        {
            yield return null;
            FindCooldownBar();
        }

        /// <summary>
        /// Reads shooting stats from the assigned ProjectileObject SO.
        /// Call this again at runtime if the projectile type is swapped (e.g. tuning scene).
        /// </summary>
        private void InitializeFromProjectile()
        {
            if (projectileType == null)
            {
                return;
            }

            // FireRate: time between shots (lower = faster)
            fireRate = projectileType.FireRate;

            // CooldownDuration: how long full recharge takes after overheat
            // regenRate = MAX_CHARGE / CooldownDuration
            // e.g. CooldownDuration=2s → regenRate=50/s → takes exactly 2s to recharge
            chargeRegenRate = MAX_CHARGE / Mathf.Max(projectileType.CooldownDuration, 0.1f);

            Debug.Log($"[{nameof(CarShooter)}] {gameObject.name} initialized from {projectileType.ProjectileName}: " +
                      $"FireRate={fireRate}s, CooldownDuration={projectileType.CooldownDuration}s, " +
                      $"RegenRate={chargeRegenRate}/s");
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
            currentCharge = MAX_CHARGE;
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
                Debug.Log($"[{nameof(CarShooter)}] {gameObject.name} - Cooldown bar already assigned");
                return;
            }

            Debug.Log($"[{nameof(CarShooter)}] {gameObject.name} - Searching for CooldownBarUI under player...");
            
            Debug.Log($"[{nameof(CarShooter)}] === HIERARCHY DEBUG START ===");
            Debug.Log($"[{nameof(CarShooter)}] Searching under: {gameObject.name}");
            Debug.Log($"[{nameof(CarShooter)}] Child count: {transform.childCount}");
            
            for (int i = 0; i < transform.childCount; i++)
            {
                Transform child = transform.GetChild(i);
                Debug.Log($"[{nameof(CarShooter)}]   Child {i}: {child.name} (children: {child.childCount})");
                
                for (int j = 0; j < child.childCount; j++)
                {
                    Transform grandchild = child.GetChild(j);
                    Debug.Log($"[{nameof(CarShooter)}]     Grandchild {j}: {grandchild.name} (children: {grandchild.childCount})");
                    
                    for (int k = 0; k < grandchild.childCount; k++)
                    {
                        Transform greatGrandchild = grandchild.GetChild(k);
                        Debug.Log($"[{nameof(CarShooter)}]       GreatGrandchild {k}: {greatGrandchild.name}");
                        
                        CooldownBarUI barUI = greatGrandchild.GetComponent<CooldownBarUI>();
                        if (barUI != null)
                        {
                            Debug.Log($"[{nameof(CarShooter)}]       ^^^ HAS CooldownBarUI COMPONENT!");
                        }
                    }
                }
            }
            Debug.Log($"[{nameof(CarShooter)}] === HIERARCHY DEBUG END ===");
            
            cooldownBar = GetComponentInChildren<CooldownBarUI>();

            if (cooldownBar != null)
            {
                Debug.Log($"[{nameof(CarShooter)}] ✓✓✓ {gameObject.name} - FOUND cooldown bar: {cooldownBar.gameObject.name}");
            }
            else
            {
                Debug.LogError($"[{nameof(CarShooter)}] ✗✗✗ {gameObject.name} - NO CooldownBarUI found under player! Cooldown bar will not display.");
                
                Transform healthBarContainer = transform.Find("HealthBarContainer");
                if (healthBarContainer != null)
                {
                    Debug.Log($"[{nameof(CarShooter)}] DEBUG: Found HealthBarContainer with {healthBarContainer.childCount} children");
                }
                else
                {
                    Debug.LogError($"[{nameof(CarShooter)}] DEBUG: HealthBarContainer NOT FOUND!");
                }
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
            RegenerateCharge();
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

        private void RegenerateCharge()
        {
            if (currentCharge >= MAX_CHARGE)
            {
                return;
            }

            currentCharge += chargeRegenRate * Time.deltaTime;
            currentCharge = Mathf.Min(currentCharge, MAX_CHARGE);
            
            if (currentCharge >= MAX_CHARGE && isOverheated)
            {
                isOverheated = false;
                Debug.Log($"<color=green>[{nameof(CarShooter)}] {gameObject.name} - ✓ COOLED DOWN! Weapon unlocked!</color>");
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

        private void UpdateCooldownBar()
        {
            if (cooldownBar == null)
            {
                return;
            }
            
            cooldownBar.UpdateCooldown(currentCharge, MAX_CHARGE);
        }

        private void FixedUpdate()
        {
            if (GameplayIsDisabled())
            {
                return;
            }
            
            if (PlayerPressingFireButton() && CanShootNow())
            {
                float chargeAfterShot = currentCharge - CHARGE_PER_SHOT;
                bool willOverheat = chargeAfterShot < CHARGE_PER_SHOT;
                
                FireProjectile();
                currentCharge -= CHARGE_PER_SHOT;
                
                if (willOverheat)
                {
                    currentCharge = 0f;
                    isOverheated = true;
                }
                
                nextAllowedFireTime = Time.time + fireRate;
            }
        }

        private bool PlayerPressingFireButton()
        {
            return isFiring;
        }

        private bool CanShootNow()
        {
            if (projectileType == null)
            {
                return false;
            }
            
            if (isOverheated)
            {
                return false;
            }
            
            bool hasEnoughCharge = currentCharge >= CHARGE_PER_SHOT;
            bool fireRateReady = Time.time >= nextAllowedFireTime;
            
            return hasEnoughCharge && fireRateReady;
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
            
            Vector3 horizontalShootDirection = new Vector3(shootDirection.x, 0f, shootDirection.z).normalized;
            Vector3 recoilDirection = -horizontalShootDirection;
            
            Vector3 horizontalVelocity = new Vector3(carRigidbody.linearVelocity.x, 0f, carRigidbody.linearVelocity.z);
            float velocityAlongRecoil = Vector3.Dot(horizontalVelocity, recoilDirection);
            
            float recoilScale = 1f;
            if (velocityAlongRecoil < 0f)
            {
                recoilScale = 0.5f;
            }
            
            Vector3 recoilForce = recoilDirection * projectileType.RecoilForce * recoilScale;
            carRigidbody.AddForce(recoilForce, ForceMode.Impulse);
        }

        public void EnableGameplay()
        {
            gameplayEnabled = true;
            CenterReticleInViewport();
            
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
            
            string tag = gameObject.tag;
            int playerNumber = 1;
            if (tag.Contains("One")) playerNumber = 1;
            else if (tag.Contains("Two")) playerNumber = 2;
            else if (tag.Contains("Three")) playerNumber = 3;
            else if (tag.Contains("Four")) playerNumber = 4;
            
            Vector2 targetPosition = Vector2.zero;

            if (playerCount == 1)
            {
                targetPosition = new Vector2(0.5f, 0.5f);
            }
            else if (playerCount == 2)
            {
                targetPosition = playerNumber == 1 ? new Vector2(-480, 0) : new Vector2(480, 0);
            }
            else if (playerCount == 3 || playerCount == 4)
            {
                if (playerNumber == 1)
                    targetPosition = new Vector2(-480, 270);
                else if (playerNumber == 2)
                    targetPosition = new Vector2(480, 270);
                else if (playerNumber == 3)
                    targetPosition = new Vector2(-480, -270);
                else
                    targetPosition = new Vector2(480, -270);
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

        /// <summary>
        /// Swap the projectile type at runtime (e.g. from TuningManager).
        /// Immediately re-initializes all shooting stats from the new SO.
        /// </summary>
        public void SetProjectileType(ProjectileObject newProjectile)
        {
            if (newProjectile == null)
            {
                Debug.LogWarning($"[{nameof(CarShooter)}] Cannot set null projectile type!");
                return;
            }

            projectileType = newProjectile;
            InitializeFromProjectile();
            
            // Reset charge state so new weapon starts fresh
            currentCharge = MAX_CHARGE;
            isOverheated = false;
            nextAllowedFireTime = 0f;

            Debug.Log($"[{nameof(CarShooter)}] {gameObject.name} swapped to {newProjectile.ProjectileName}");
        }
    }
}