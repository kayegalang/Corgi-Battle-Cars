using UnityEngine;

namespace Player.Scripts
{
    public class CarController : MonoBehaviour
    {
        private PlayerControls controls;
        private Vector2 moveInput;

        private Rigidbody rb;

        [Header("Car Settings")] public float acceleration = 15f;
        public float turnSpeed = 50f;

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
            // Forward/back movement
            float forward = moveInput.y * acceleration;
            Vector3 force = transform.forward * forward;
            rb.AddForce(force, ForceMode.Force);

            // Turning (only while moving forward/backward a bit)
            if (rb.linearVelocity.magnitude > 0.1f)
            {
                float turn = moveInput.x * turnSpeed * Time.fixedDeltaTime;
                Quaternion turnRotation = Quaternion.Euler(0f, turn, 0f);
                rb.MoveRotation(rb.rotation * turnRotation);
            }
            
            
        }
    }
} 