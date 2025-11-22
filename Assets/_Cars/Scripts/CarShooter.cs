using _Projectiles.Scripts;
using UnityEngine;
using UnityEngine.InputSystem;

namespace _Cars.Scripts
{
    public class CarShooter : MonoBehaviour
    {
        private PlayerControls controls;
        private Rigidbody carRb;

        [Header("Reticle Settings")]
        [SerializeField] private RectTransform reticle;
        [SerializeField] private Canvas reticleCanvas;        
        [SerializeField] private float controllerSensitivity = 900f;
        [SerializeField] private Camera playerCamera;         

        private Vector2 controllerAim;
        private bool usingMouse = true;

        [Header("Projectile Settings")]
        public GameObject projectilePrefab;
        [SerializeField] private float fireForce = 30f;
        [SerializeField] private float fireRate = 0.25f;

        private float nextFire = 0f;
        private bool isFiring;
        private Transform firePoint;
        
        void Awake()
        {
            controls = new PlayerControls();
            carRb = GetComponent<Rigidbody>();
            firePoint = transform.Find("FirePoint");

            Cursor.visible = false;
        }

        private void OnEnable()
        {
            controls.Gameplay.Enable();

            // Shooting input
            controls.Gameplay.Shoot.performed += ctx => isFiring = true;
            controls.Gameplay.Shoot.canceled += ctx => isFiring = false;

            // Controller aim
            controls.Gameplay.Aim.performed += ctx =>
            {
                controllerAim = ctx.ReadValue<Vector2>();
                if (controllerAim.sqrMagnitude > 0.1f)
                    usingMouse = false;
            };

            controls.Gameplay.Aim.canceled += ctx => controllerAim = Vector2.zero;
        }

        private void OnDisable()
        {
            controls.Gameplay.Disable();
        }

        void Update()
        {
            UpdateReticle();
        }

        void FixedUpdate()
        {
            if (isFiring && Time.time > nextFire)
            {
                Shoot();
                nextFire = Time.time + fireRate;
            }
        }
        
        private void UpdateReticle()
        {
            if (usingMouse)
                MoveReticleWithMouse();
            else
                MoveReticleWithController();
        }

        private void MoveReticleWithMouse()
        {
            // Global mouse position → THIS player's viewport
            Vector2 mousePos = Mouse.current.position.ReadValue();

            Vector2 viewport = playerCamera.ScreenToViewportPoint(mousePos);

            // Convert viewport (0–1) into canvas local space
            RectTransform canvasRect = (RectTransform)reticleCanvas.transform;

            reticle.anchoredPosition = new Vector2(
                (viewport.x - 0.5f) * canvasRect.sizeDelta.x,
                (viewport.y - 0.5f) * canvasRect.sizeDelta.y
            );
        }

        private void MoveReticleWithController()
        {
            RectTransform canvasRect = (RectTransform)reticleCanvas.transform;

            reticle.anchoredPosition += controllerAim * controllerSensitivity * Time.deltaTime;

            reticle.anchoredPosition = ClampToCanvas(reticle.anchoredPosition, canvasRect);
        }

        private Vector2 ClampToCanvas(Vector2 pos, RectTransform canvas)
        {
            float halfW = canvas.sizeDelta.x / 2f;
            float halfH = canvas.sizeDelta.y / 2f;

            pos.x = Mathf.Clamp(pos.x, -halfW, halfW);
            pos.y = Mathf.Clamp(pos.y, -halfH, halfH);

            return pos;
        }

        private void Shoot()
        {
            Vector3 shootDir = GetDirectionFromReticle();

            GameObject bullet = Instantiate(projectilePrefab, firePoint.position, 
                                            Quaternion.LookRotation(shootDir));

            Projectile proj = bullet.GetComponent<Projectile>();
            if (proj != null)
            {
                // Pass the root GameObject which has the proper tag (Player1, Player2, etc.)
                GameObject rootObject = transform.root.gameObject;
                proj.SetShooter(rootObject);
            }

            Rigidbody rb = bullet.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = carRb.linearVelocity;
                rb.AddForce(shootDir * fireForce, ForceMode.Impulse);
            }
        }

        private Vector3 GetDirectionFromReticle()
        {
            RectTransform canvasRect = (RectTransform)reticleCanvas.transform;

            // Convert anchored pos back into viewport coordinates
            Vector2 normalizedViewport = new Vector2(
                (reticle.anchoredPosition.x / canvasRect.sizeDelta.x) + 0.5f,
                (reticle.anchoredPosition.y / canvasRect.sizeDelta.y) + 0.5f
            );

            Ray ray = playerCamera.ViewportPointToRay(normalizedViewport);

            if (Physics.Raycast(ray, out RaycastHit hit))
                return (hit.point - firePoint.position).normalized;

            return (ray.GetPoint(50f) - firePoint.position).normalized;
        }
    }
}