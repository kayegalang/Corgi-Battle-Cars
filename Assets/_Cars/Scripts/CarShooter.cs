using _Projectiles.Scripts;
using _Gameplay.Scripts;
using UI.Scripts;
using UnityEngine;
using UnityEngine.InputSystem;

namespace _Cars.Scripts
{
    public class CarShooter : MonoBehaviour
    {
        private PlayerInput playerInput;
        private InputAction shootAction;
        private InputAction aimAction;
        
        private Rigidbody carRb;
        private PauseController pauseController;
        private Camera playerCamera; // Store reference to this player's camera
        
        [Header("Gameplay Control")]
        [SerializeField] private bool gameplayEnabled = false; // Toggle this to enable/disable gameplay

        [Header("Reticle Settings")]
        [SerializeField] private RectTransform reticle;
        [SerializeField] private Canvas reticleCanvas;
        [SerializeField] private float controllerSensitivity = 900f;
        [SerializeField] private float mouseSensitivity = 1.5f;

        private Vector2 controllerAim;
        private Vector2 lastMousePosition;
        private bool usingMouse = true;

        [Header("Projectile")]
        public GameObject projectilePrefab;
        [SerializeField] private float fireForce = 30f;
        [SerializeField] private float fireRate = 0.25f;

        private float nextFire = 0f;
        private bool isFiring;
        private Transform firePoint;


        void Awake()
        {
            playerInput = GetComponent<PlayerInput>();
            
            var actions = playerInput.actions;
            shootAction = actions.FindAction("Shoot", true);
            aimAction = actions.FindAction("Aim", true);
            
            carRb = GetComponent<Rigidbody>();
            firePoint = transform.Find("FirePoint");
            
            // Find this player's camera (not Camera.main)
            playerCamera = GetComponentInChildren<Camera>();
            
            pauseController = FindFirstObjectByType<PauseController>();

            // Start with reticle hidden (gameplay not enabled yet)
            if (reticle != null)
                reticle.gameObject.SetActive(false);
            
            // Initialize last mouse position
            lastMousePosition = Mouse.current.position.ReadValue();
        }

        private void OnEnable()
        {
            shootAction.Enable();
            aimAction.Enable();

            // Shoot
            shootAction.performed += OnShootPerformed;
            shootAction.canceled += OnShootCanceled;

            // Controller Aim
            aimAction.performed += OnAimPerformed;
            aimAction.canceled += OnAimCanceled;
        }

        private void OnDisable()
        {
            shootAction.Disable();
            aimAction.Disable();
            
            // Unsubscribe from callbacks
            shootAction.performed -= OnShootPerformed;
            shootAction.canceled -= OnShootCanceled;
            aimAction.performed -= OnAimPerformed;
            aimAction.canceled -= OnAimCanceled;
            
            // Hide reticle when disabled (e.g., when game ends)
            if (reticle != null)
                reticle.gameObject.SetActive(false);
            
            // Show and unlock cursor when disabled
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
        
        private void OnShootPerformed(InputAction.CallbackContext ctx)
        {
            isFiring = true;
        }
        
        private void OnShootCanceled(InputAction.CallbackContext ctx)
        {
            isFiring = false;
        }
        
        private void OnAimPerformed(InputAction.CallbackContext ctx)
        {
            controllerAim = ctx.ReadValue<Vector2>();
            if (controllerAim.sqrMagnitude > 0.1f)
                usingMouse = false;
        }
        
        private void OnAimCanceled(InputAction.CallbackContext ctx)
        {
            controllerAim = Vector2.zero;
        }

        void Update()
        {
            // If gameplay not enabled, keep reticle hidden
            if (!gameplayEnabled)
            {
                if (reticle != null)
                    reticle.gameObject.SetActive(false);
                return;
            }
            
            // Check if game has ended - if so, keep cursor visible and don't update reticle
            if (GameplayManager.instance != null && GameplayManager.instance.IsGameEnded())
            {
                SetCursorState(true); // Show cursor, hide reticle
                return;
            }
            
            // Check if game is paused and handle reticle/cursor accordingly
            if (pauseController != null && pauseController.GetIsPaused())
            {
                SetCursorState(true); // Show cursor, hide reticle
            }
            else
            {
                SetCursorState(false); // Hide cursor, show reticle
                UpdateReticle();
            }
        }

        void FixedUpdate()
        {
            // Don't shoot if gameplay not enabled
            if (!gameplayEnabled)
                return;
            
            if (isFiring && Time.time > nextFire)
            {
                Shoot();
                nextFire = Time.time + fireRate;
            }
        }


        // ============================================================
        //  RETICLE SYSTEM
        // ============================================================

        private void UpdateReticle()
        {
            if (usingMouse)
                MoveReticleWithMouse();
            else
                MoveReticleWithController();
        }

        private void MoveReticleWithMouse()
        {
            RectTransform canvasRect = reticleCanvas.transform as RectTransform;
            
            // Get current mouse position
            Vector2 currentMousePosition = Mouse.current.position.ReadValue();
            
            // Calculate mouse delta (how much the mouse moved)
            Vector2 mouseDelta = currentMousePosition - lastMousePosition;
            
            // Update last mouse position
            lastMousePosition = currentMousePosition;
            
            // Move reticle based on mouse delta with sensitivity
            reticle.anchoredPosition += mouseDelta * mouseSensitivity;
            
            // Clamp to canvas bounds
            reticle.anchoredPosition = ClampToCanvas(reticle.anchoredPosition, canvasRect);
        }

        private void MoveReticleWithController()
        {
            RectTransform canvasRect = reticleCanvas.transform as RectTransform;

            // Move reticle based on right stick input
            reticle.anchoredPosition += controllerAim * controllerSensitivity * Time.deltaTime;

            reticle.anchoredPosition = ClampToCanvas(reticle.anchoredPosition, canvasRect);
        }

        private Vector2 ClampToCanvas(Vector2 pos, RectTransform canvas)
        {
            // Get camera viewport in screen pixels
            Rect viewportRect = playerCamera.pixelRect;
            
            // Convert canvas anchored position to screen position
            Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(null, reticle.position);
            
            // Clamp screen position to camera viewport
            screenPos.x = Mathf.Clamp(screenPos.x, viewportRect.xMin, viewportRect.xMax);
            screenPos.y = Mathf.Clamp(screenPos.y, viewportRect.yMin, viewportRect.yMax);
            
            // Convert back to canvas anchored position
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvas,
                screenPos,
                null,
                out Vector2 localPoint
            );
            
            return localPoint;
        }


        // ============================================================
        //  SHOOTING SYSTEM
        // ============================================================

        private void Shoot()
        {
            Vector3 shootDir = GetDirectionFromReticle();

            GameObject bullet = Instantiate(projectilePrefab, firePoint.position, Quaternion.LookRotation(shootDir));

            Projectile proj = bullet.GetComponent<Projectile>();
            if (proj != null)
                proj.SetShooter(gameObject);

            Rigidbody bulletRb = bullet.GetComponent<Rigidbody>();
            if (bulletRb != null)
            {
                bulletRb.linearVelocity = carRb.linearVelocity; // inherit movement
                bulletRb.AddForce(shootDir * fireForce, ForceMode.Impulse);
            }
        }

        private Vector3 GetDirectionFromReticle()
        {
            // Convert UI reticle position → screen position
            Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(null, reticle.position);

            // Ray from THIS PLAYER'S camera to reticle (not Camera.main)
            Ray ray = playerCamera.ScreenPointToRay(screenPos);

            if (Physics.Raycast(ray, out RaycastHit hit))
                return (hit.point - firePoint.position).normalized;

            return (ray.GetPoint(50f) - firePoint.position).normalized;
        }
        
        // ============================================================
        //  CURSOR & RETICLE STATE
        // ============================================================
        
        public void EnableGameplay()
        {
            gameplayEnabled = true;
            Debug.Log($"{gameObject.name}: Gameplay enabled!");
        }
        
        public void DisableGameplay()
        {
            gameplayEnabled = false;
            SetCursorState(true); // Show cursor when disabled
        }
        
        private void SetCursorState(bool showCursor)
        {
            Cursor.visible = showCursor;
            
            if (showCursor)
            {
                Cursor.lockState = CursorLockMode.None;
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
            }
            
            if (reticle != null)
                reticle.gameObject.SetActive(!showCursor);
        }
        
        public void DisableReticle()
        {
            SetCursorState(true);
        }
    }
}