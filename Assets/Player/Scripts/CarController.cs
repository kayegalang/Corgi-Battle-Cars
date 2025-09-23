using UnityEngine;

namespace Player.Scripts
{
    public class CarController : MonoBehaviour
    {
        private PlayerControls controls;
        private Vector2 moveInput;

        private Rigidbody rb;

        [Header("Car Settings")] public float acceleration = 15f;
        public float turnSpeed = 20f;

        private void Awake()
        {
            controls = new PlayerControls();
            rb = GetComponent<Rigidbody>();
        }

        private void OnEnable()
        {
            controls.Gameplay.Enable();
            controls.Gameplay.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
            controls.Gameplay.Move.canceled += ctx => moveInput = Vector2.zero;
        }

        private void OnDisable()
        {
            controls.Gameplay.Disable();
        }

        private void FixedUpdate()
        {
            Move();
            Turn();
        }

        private void Turn()
        {
            if (moveInput.x > 0)
            {
                rb.AddTorque(UnityEngine.Vector3.up * turnSpeed);
            }
            else if (moveInput.x < 0)
            {
                rb.AddTorque(-UnityEngine.Vector3.up * turnSpeed);
            }
        }

        private void Move()
        {
            if (moveInput.y > 0)
            {
                rb.AddRelativeForce(UnityEngine.Vector3.forward * acceleration);
            }
            else if (moveInput.y < 0)
            {
                rb.AddRelativeForce(-UnityEngine.Vector3.forward * acceleration);
            }

            Vector3 localVelocity = transform.InverseTransformDirection(rb.linearVelocity);
            localVelocity.x = 0;
            rb.linearVelocity = transform.TransformDirection(localVelocity);
        }
    }
} 