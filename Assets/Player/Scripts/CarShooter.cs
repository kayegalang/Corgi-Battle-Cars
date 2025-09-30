using UnityEngine;
using UnityEngine.InputSystem;

namespace Player.Scripts
{
    public class CarShooter : MonoBehaviour
    {
        private PlayerControls controls;
        private Rigidbody carRb;

        public GameObject projectilePrefab;
        [SerializeField] private Transform firePoint;
        [SerializeField] private float fireForce = 30f;
        [SerializeField] private float fireRate = 0.25f;
        private bool isFiring = false;
        private float nextFireTime = 0f;

        void Awake()
        {
            controls = new PlayerControls();
            carRb = GetComponent<Rigidbody>();
        }

        private void OnEnable()
        {
            controls.Gameplay.Enable();
            
            // Shoot
            controls.Gameplay.Shoot.performed += ctx => isFiring = true;
            controls.Gameplay.Shoot.canceled += ctx => isFiring = false;
        }
        
        private void OnDisable()
        {
            controls.Gameplay.Disable();
        }

        private void FixedUpdate()
        {
            if (isFiring && Time.time > nextFireTime)
            {
                Shoot();
                nextFireTime = Time.time + fireRate;
            }
        }
        
        private void Shoot()
        {
            Vector3 shootDirection = GetDirection();

            GameObject bullet = Instantiate(projectilePrefab, firePoint.position, Quaternion.LookRotation(shootDirection));

            Projectile proj = bullet.GetComponent<Projectile>();
            if (proj != null)
            {
                proj.SetShooter(gameObject); // "gameObject" = the car that fired
            }
            
            Rigidbody bulletRb = bullet.GetComponent<Rigidbody>();
            if (bulletRb != null)
            {
                bulletRb.linearVelocity = carRb.linearVelocity; // inherit car’s current movement
                bulletRb.AddForce(shootDirection * fireForce, ForceMode.Impulse); // add firing force
            }

            bullet.GetComponent<Rigidbody>().AddForce(shootDirection * fireForce, ForceMode.Impulse);
        }

        private Vector3 GetDirection()
        {
            Vector2 mouseScreenPos = Mouse.current.position.ReadValue();
            Ray ray = Camera.main.ScreenPointToRay(mouseScreenPos);

            Vector3 targetPoint;

            if (Physics.Raycast(ray, out RaycastHit hit))
                targetPoint = hit.point;
            else
                targetPoint = ray.GetPoint(50f);

            Vector3 shootDirection = (targetPoint - firePoint.position).normalized;
            return shootDirection;
        }
    }
}
