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

        private Vector2 controllerAim;
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
            controls = new PlayerControls();
            carRb = GetComponent<Rigidbody>();
            firePoint = transform.Find("FirePoint");

            // Hide OS cursor
            Cursor.visible = false;
        }

        private void OnEnable()
        {
            controls.Gameplay.Enable();

            // Shoot
            controls.Gameplay.Shoot.performed += ctx => isFiring = true;
            controls.Gameplay.Shoot.canceled += ctx => isFiring = false;

            // Controller Aim
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
            RectTransform canvasRectTransform = reticleCanvas.transform as RectTransform;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRectTransform,
                Mouse.current.position.ReadValue(),
                null,
                out Vector2 localPoint
            );

            reticle.anchoredPosition = localPoint;

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
            float halfW = canvas.sizeDelta.x / 2f;
            float halfH = canvas.sizeDelta.y / 2f;

            pos.x = Mathf.Clamp(pos.x, -halfW, halfW);
            pos.y = Mathf.Clamp(pos.y, -halfH, halfH);

            return pos;
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

            // Ray from camera to reticle
            Ray ray = Camera.main.ScreenPointToRay(screenPos);

            if (Physics.Raycast(ray, out RaycastHit hit))
                return (hit.point - firePoint.position).normalized;

            return (ray.GetPoint(50f) - firePoint.position).normalized;
        }
    }
}