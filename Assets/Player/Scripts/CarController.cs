using Gameplay.Scripts;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Player.Scripts
{
    public class CarController : MonoBehaviour
    {
        private PlayerControls controls;
        private Vector2 moveInput;

        private Rigidbody carRb;

        [Header("Car Settings")]
        [SerializeField] private float acceleration = 20f;
        [SerializeField] private float turnSpeed = 20f;
        
        [SerializeField] private Vector3 groundCheckOffset = new Vector3(0f, 0.26f, 0f);
        [SerializeField] private float groundCheckDistance = 0.26f;
        [SerializeField] private float jumpForce = 5f;
        
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

        void Update()
        {
            if (controls.UI.Pause.triggered) 
            {
                if (GameplayManager.instance.GetIsPaused())
                {
                    GameplayManager.instance.UnpauseGame();
                }
                else
                {
                    GameplayManager.instance.PauseGame();
                }
            }

        }

        private void OnEnable()
        {
            controls.Gameplay.Enable();
            controls.UI.Enable();
            
            // Movement
            controls.Gameplay.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
            controls.Gameplay.Move.canceled += ctx => moveInput = Vector2.zero;
            
            // Jump
            controls.Gameplay.Jump.performed += ctx => Jump();
            
            // Shoot
            controls.Gameplay.Shoot.performed += ctx => isFiring = true;
            controls.Gameplay.Shoot.canceled += ctx => isFiring = false;

        }

        private void OnDisable()
        {
            controls.Gameplay.Disable();
            controls.UI.Disable();
        }

        private void FixedUpdate()
        {
            Move();
            Turn();
            
            // Ensures air has more force
            if (IsGrounded())
            {
                carRb.angularDamping = 3;
            }
            else
            {
                carRb.angularDamping = 5; 
                
                Quaternion levelRotation = Quaternion.Euler(0, carRb.rotation.eulerAngles.y, 0);
                carRb.MoveRotation(Quaternion.Slerp(carRb.rotation, levelRotation, 2f * Time.fixedDeltaTime));
            }

            if (isFiring && Time.time > nextFireTime)
            {
                Shoot();
                nextFireTime = Time.time + fireRate;
            }

        }

        private void Turn()
        {
            if (IsMovingForward())
            {
                if (moveInput.x > 0)
                {
                    carRb.AddTorque(UnityEngine.Vector3.up * turnSpeed);
                }
                else if (moveInput.x < 0)
                {
                    carRb.AddTorque(-UnityEngine.Vector3.up * turnSpeed);
                }
            }
            else if (!IsMovingForward())
            {
                if (moveInput.x > 0)
                {
                    carRb.AddTorque(-UnityEngine.Vector3.up * turnSpeed);
                }
                else if (moveInput.x < 0)
                {
                    carRb.AddTorque(UnityEngine.Vector3.up * turnSpeed);
                }
            }

        }

        private void Move()
        {
            if (moveInput.y > 0)
            {
                carRb.AddRelativeForce(UnityEngine.Vector3.forward * acceleration);
            }
            else if (moveInput.y < 0)
            {
                carRb.AddRelativeForce(-UnityEngine.Vector3.forward * acceleration);
            }

            Vector3 localVelocity = transform.InverseTransformDirection(carRb.linearVelocity);
            localVelocity.x = 0;
            carRb.linearVelocity = transform.TransformDirection(localVelocity);
        }

        private bool IsMovingForward()
        {
            return moveInput.y >= 0;
        }

        private bool IsGrounded()
        {
            RaycastHit hit;
            
            Vector3 origin = transform.position + groundCheckOffset;

            Debug.DrawRay(origin, -transform.up * groundCheckDistance, Color.red);
            
            return Physics.Raycast(origin, -transform.up, out hit, groundCheckDistance);
        }

        private void Jump()
        {
            if (IsGrounded())
            {
                carRb.AddForce(transform.up * jumpForce, ForceMode.Impulse);
            }
        }
        
        private void Shoot()
        {
            Vector2 mouseScreenPos = Mouse.current.position.ReadValue();
            Ray ray = Camera.main.ScreenPointToRay(mouseScreenPos);

            Vector3 targetPoint;

            if (Physics.Raycast(ray, out RaycastHit hit))
                targetPoint = hit.point;
            else
                targetPoint = ray.GetPoint(50f);

            Vector3 shootDirection = (targetPoint - firePoint.position).normalized;

            GameObject bullet = Instantiate(projectilePrefab, firePoint.position, Quaternion.LookRotation(shootDirection));

            Rigidbody bulletRb = bullet.GetComponent<Rigidbody>();
            if (bulletRb != null)
            {
                bulletRb.linearVelocity = carRb.linearVelocity; // inherit car’s current movement
                bulletRb.AddForce(shootDirection * fireForce, ForceMode.Impulse); // add firing force
            }

            bullet.GetComponent<Rigidbody>().AddForce(shootDirection * fireForce, ForceMode.Impulse);
            
            Projectile proj = bullet.GetComponent<Projectile>();
            if (proj != null)
            {
                proj.SetShooter(gameObject); // "gameObject" = the car that fired
            }
        }
    }
} 