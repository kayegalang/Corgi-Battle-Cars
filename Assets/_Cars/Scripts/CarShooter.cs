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
        
        private float currentCharge;
        private float maxCharge = 100f;
        private float chargePerShot = 10f; // How much charge each shot costs
        private float chargeRegenRate = 20f; // Charge regenerated per second
        private float fireRate = 0.1f; // Minimum time between shots (10 shots/sec max)
        private float nextAllowedFireTime;
        private bool isOverheated = false; // Locks shooting until fully recharged

        private void Awake()
        {
            CacheComponents();
            BindInputActions();
            InitializeReticleState();
            CaptureInitialMousePosition();
            ValidateProjectileType();
            // Don't find cooldown bar yet - HealthBarManager creates it in Start()
        }

        private void Start()
        {
            // Delay one frame to let HealthBarManager create PlayerHealthCanvas
            StartCoroutine(FindCooldownBarDelayed());
        }

        private System.Collections.IEnumerator FindCooldownBarDelayed()
        {
            yield return null; // Wait one frame
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
            currentCharge = maxCharge; // Start with full charge
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
            
            // Debug: Show ALL children of this player
            Debug.Log($"[{nameof(CarShooter)}] === HIERARCHY DEBUG START ===");
            Debug.Log($"[{nameof(CarShooter)}] Searching under: {gameObject.name}");
            Debug.Log($"[{nameof(CarShooter)}] Child count: {transform.childCount}");
            
            for (int i = 0; i < transform.childCount; i++)
            {
                Transform child = transform.GetChild(i);
                Debug.Log($"[{nameof(CarShooter)}]   Child {i}: {child.name} (children: {child.childCount})");
                
                // Show grandchildren too
                for (int j = 0; j < child.childCount; j++)
                {
                    Transform grandchild = child.GetChild(j);
                    Debug.Log($"[{nameof(CarShooter)}]     Grandchild {j}: {grandchild.name} (children: {grandchild.childCount})");
                    
                    // Show great-grandchildren
                    for (int k = 0; k < grandchild.childCount; k++)
                    {
                        Transform greatGrandchild = grandchild.GetChild(k);
                        Debug.Log($"[{nameof(CarShooter)}]       GreatGrandchild {k}: {greatGrandchild.name}");
                        
                        // Check if this has CooldownBarUI
                        CooldownBarUI barUI = greatGrandchild.GetComponent<CooldownBarUI>();
                        if (barUI != null)
                        {
                            Debug.Log($"[{nameof(CarShooter)}]       ^^^ HAS CooldownBarUI COMPONENT!");
                        }
                    }
                }
            }
            Debug.Log($"[{nameof(CarShooter)}] === HIERARCHY DEBUG END ===");
            
            // Now search for CooldownBarUI
            cooldownBar = GetComponentInChildren<CooldownBarUI>();

            if (cooldownBar != null)
            {
                Debug.Log($"[{nameof(CarShooter)}] ✓✓✓ {gameObject.name} - FOUND cooldown bar: {cooldownBar.gameObject.name}");
            }
            else
            {
                Debug.LogError($"[{nameof(CarShooter)}] ✗✗✗ {gameObject.name} - NO CooldownBarUI found under player! Cooldown bar will not display.");
                
                // Extra debug: Try to find HealthBarContainer
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
            if (currentCharge < maxCharge)
            {
                currentCharge += chargeRegenRate * Time.deltaTime;
                currentCharge = Mathf.Min(currentCharge, maxCharge);
                
                // Only clear overheat when FULLY recharged
                if (currentCharge >= maxCharge && isOverheated)
                {
                    isOverheated = false;
                    Debug.Log($"<color=green>[{nameof(CarShooter)}] {gameObject.name} - ✓✓✓ COOLED DOWN! Weapon unlocked! Can shoot again!</color>");
                }
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
                // Only log this once per second to avoid spam
                if (Time.frameCount % 60 == 0)
                {
                    Debug.LogError($"[{nameof(CarShooter)}] {gameObject.name} - Cooldown bar is NULL! Not found during Start!");
                }
                return;
            }
            
            // Show current charge out of max charge
            // When charge is full (100), bar is blue (ready)
            // When charge is low/empty, bar is red (can't shoot)
            
            if (Time.frameCount % 30 == 0) // Debug every 30 frames
            {
                string overheatStatus = isOverheated ? "🔥 OVERHEATED!" : "✓ OK";
                Debug.Log($"[{nameof(CarShooter)}] {gameObject.name} - Charge: {currentCharge:F1}/{maxCharge}, fillPercent={(currentCharge / maxCharge) * 100:F0}%, Status: {overheatStatus}");
            }
            
            cooldownBar.UpdateCooldown(currentCharge, maxCharge);
        }

        private void FixedUpdate()
        {
            if (GameplayIsDisabled())
            {
                return;
            }
            
            if (PlayerPressingFireButton() && CanShootNow())
            {
                // Check if after THIS shot, we won't have enough for ANOTHER shot
                // This prevents regen from keeping us barely above 0
                float chargeAfterShot = currentCharge - chargePerShot;
                bool willOverheat = chargeAfterShot < chargePerShot;
                
                Debug.Log($"[{nameof(CarShooter)}] {gameObject.name} - SHOOTING! Charge: {currentCharge:F1} → {chargeAfterShot:F1}, WillOverheat: {willOverheat}");
                FireProjectile();
                currentCharge -= chargePerShot;
                
                // Overheat if we can't shoot again
                if (willOverheat)
                {
                    currentCharge = 0f; // Clamp to 0
                    isOverheated = true;
                    Debug.Log($"<color=red>[{nameof(CarShooter)}] {gameObject.name} - ⚠️⚠️⚠️ OVERHEATED! Weapon locked until fully recharged!</color>");
                }
                
                nextAllowedFireTime = Time.time + fireRate;
            }
            else if (PlayerPressingFireButton() && isOverheated)
            {
                // Log when trying to shoot while overheated (every 30 frames to avoid spam)
                if (Time.frameCount % 30 == 0)
                {
                    Debug.LogWarning($"[{nameof(CarShooter)}] {gameObject.name} - ❌ Can't shoot! OVERHEATED! Charge: {currentCharge:F1}/100");
                }
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
            
            // Cannot shoot if overheated - must wait for full recharge!
            if (isOverheated)
            {
                return false;
            }
            
            // Need BOTH enough charge AND enough time has passed
            bool hasEnoughCharge = currentCharge >= chargePerShot;
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
            
            // Only apply horizontal recoil (ignore vertical component)
            Vector3 horizontalShootDirection = new Vector3(shootDirection.x, 0f, shootDirection.z).normalized;
            Vector3 recoilDirection = -horizontalShootDirection;
            
            // Reduce recoil slightly if moving opposite to recoil direction (allows acceleration)
            Vector3 horizontalVelocity = new Vector3(carRigidbody.linearVelocity.x, 0f, carRigidbody.linearVelocity.z);
            float velocityAlongRecoil = Vector3.Dot(horizontalVelocity, recoilDirection);
            
            float recoilScale = 1f;
            if (velocityAlongRecoil < 0f) // Moving opposite to recoil (trying to accelerate forward)
            {
                // Reduce recoil by 50% when actively accelerating against it
                recoilScale = 0.5f;
            }
            
            Vector3 recoilForce = recoilDirection * projectileType.RecoilForce * recoilScale;
            carRigidbody.AddForce(recoilForce, ForceMode.Impulse);
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