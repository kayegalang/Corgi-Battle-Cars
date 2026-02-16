using _Projectiles.Scripts;
using _Gameplay.Scripts;
using _UI.Scripts;
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
        private Camera playerCamera; 
        
        [Header("Gameplay Control")]
        [SerializeField] private bool gameplayEnabled = false;

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
            
            playerCamera = GetComponentInChildren<Camera>();
            
            pauseController = FindFirstObjectByType<PauseController>();

            HideReticle();
            
            lastMousePosition = Mouse.current.position.ReadValue();
        }

        private void HideReticle()
        {
            if (reticle != null)
                reticle.gameObject.SetActive(false);
        }

        private void OnEnable()
        {
            shootAction.Enable();
            aimAction.Enable();

            shootAction.performed += OnShootPerformed;
            shootAction.canceled += OnShootCanceled;

            aimAction.performed += OnAimPerformed;
            aimAction.canceled += OnAimCanceled;
        }

        private void OnDisable()
        {
            shootAction.Disable();
            aimAction.Disable();
            
            shootAction.performed -= OnShootPerformed;
            shootAction.canceled -= OnShootCanceled;
            aimAction.performed -= OnAimPerformed;
            aimAction.canceled -= OnAimCanceled;
            
            if (reticle != null)
                reticle.gameObject.SetActive(false);
            
            if (IsKeyboardPlayer())
            {
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
            }
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
            if (!gameplayEnabled)
            {
                if (reticle != null)
                    reticle.gameObject.SetActive(false);
                return;
            }
            
            if (GameplayManager.instance != null && GameplayManager.instance.IsGameEnded())
            {
                SetCursorState(true);
                return;
            }
            
            if (pauseController != null && pauseController.GetIsPaused())
            {
                SetCursorState(true);
            }
            else
            {
                SetCursorState(false);
                UpdateReticle();
            }
        }

        void FixedUpdate()
        {
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
            
            Vector2 currentMousePosition = Mouse.current.position.ReadValue();
            Vector2 mouseDelta = currentMousePosition - lastMousePosition;
            lastMousePosition = currentMousePosition;
            
            reticle.anchoredPosition += mouseDelta * mouseSensitivity;
            reticle.anchoredPosition = ClampToCanvas(reticle.anchoredPosition, canvasRect);
        }

        private void MoveReticleWithController()
        {
            RectTransform canvasRect = reticleCanvas.transform as RectTransform;

            reticle.anchoredPosition += controllerAim * controllerSensitivity * Time.deltaTime;
            reticle.anchoredPosition = ClampToCanvas(reticle.anchoredPosition, canvasRect);
        }

        private Vector2 ClampToCanvas(Vector2 pos, RectTransform canvas)
        {
            Rect viewportRect = playerCamera.pixelRect;
            
            Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(null, reticle.position);
            
            screenPos.x = Mathf.Clamp(screenPos.x, viewportRect.xMin, viewportRect.xMax);
            screenPos.y = Mathf.Clamp(screenPos.y, viewportRect.yMin, viewportRect.yMax);
            
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvas,
                screenPos,
                null,
                out Vector2 localPoint
            );
            
            return localPoint;
        }
        
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
                bulletRb.linearVelocity = carRb.linearVelocity;
                bulletRb.AddForce(shootDir * fireForce, ForceMode.Impulse);
            }
        }

        private Vector3 GetDirectionFromReticle()
        {
            Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(null, reticle.position);

            Ray ray = playerCamera.ScreenPointToRay(screenPos);

            if (Physics.Raycast(ray, out RaycastHit hit))
                return (hit.point - firePoint.position).normalized;

            return (ray.GetPoint(50f) - firePoint.position).normalized;
        }
        
        public void EnableGameplay()
        {
            gameplayEnabled = true;
            CenterReticleInViewport();
        }
        
        private void CenterReticleInViewport()
        {
            if (reticle == null || playerCamera == null || reticleCanvas == null)
            {
                Debug.LogWarning($"[CarShooter] Cannot center reticle - missing components");
                return;
            }
    
            Rect viewportRect = playerCamera.rect;
            RectTransform canvasRect = reticleCanvas.GetComponent<RectTransform>();
            Vector2 canvasSize = canvasRect.sizeDelta;
    
            float centerX = (viewportRect.center.x * canvasSize.x) - (canvasSize.x * 0.5f);
            float centerY = -canvasSize.y * 0.5f;
    
            reticle.anchoredPosition = new Vector2(centerX, centerY);
    
            Debug.Log($"[CarShooter] {gameObject.name} - Reticle centered at: {reticle.anchoredPosition}");
        }
        
        public void DisableGameplay()
        {
            gameplayEnabled = false;
            SetCursorState(true); 
        }
        
        private void SetCursorState(bool showCursor)
        {
            if (!showCursor && !IsKeyboardPlayer())
                return;

            Cursor.visible = showCursor;
            Cursor.lockState = showCursor ? CursorLockMode.None : CursorLockMode.Locked;

            if (reticle != null)
                reticle.gameObject.SetActive(!showCursor);
        }

        private bool IsKeyboardPlayer()
        {
            return playerInput != null && playerInput.currentControlScheme == "Keyboard";
        }
        
        public void DisableReticle()
        {
            SetCursorState(true);
        }
    }
}