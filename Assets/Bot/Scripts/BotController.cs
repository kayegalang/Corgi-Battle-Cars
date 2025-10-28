using Gameplay.Scripts;
using Player.Scripts;
using UI.Scripts;
using Unity.VisualScripting;
using UnityEngine;

namespace Bot.Scripts
{
    public class BotController : MonoBehaviour
    {
        private Vector2 moveInput; 
        private Rigidbody carRb;
        
        [SerializeField] private CarStats carStats;

        private float acceleration;
        private float turnSpeed;
        private Vector3 groundCheckOffset;
        private float groundCheckDistance;
        private float jumpForce;
        private float maxSpeed;

        void Awake()
        {
            carRb = GetComponent<Rigidbody>();
            InitializeCarStats();
        }

        private void InitializeCarStats()
        {
            acceleration = carStats.acceleration;
            turnSpeed = carStats.turnSpeed;
            groundCheckOffset = carStats.groundCheckOffset;
            groundCheckDistance = carStats.groundCheckDistance;
            jumpForce = carStats.jumpForce;
            maxSpeed = carStats.maxSpeed;
        }

        private void FixedUpdate()
        {
            Move();
            Turn();

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
            
            CapJumpHeight();
            
            if (carRb.linearVelocity.magnitude > maxSpeed)
            {
                carRb.linearVelocity = carRb.linearVelocity.normalized * maxSpeed;
            }
        }

        private void Turn()
        {
            float turnInput = moveInput.x;
            
            if (IsMovingForward())
            {
                carRb.AddTorque(Vector3.up * turnInput * turnSpeed);
            }
            else
            {
                carRb.AddTorque(-Vector3.up * turnInput * turnSpeed);
            }
        }

        private void Move()
        {
            float moveValue = moveInput.y;

            if (Mathf.Abs(moveValue) > 0.01f)
            {
                carRb.AddRelativeForce(Vector3.forward * moveValue * acceleration);
            }

            Vector3 localVelocity = transform.InverseTransformDirection(carRb.linearVelocity);
            localVelocity.x = 0;
            carRb.linearVelocity = transform.TransformDirection(localVelocity);
        }

        private bool IsMovingForward()
        {
            return moveInput.y >= 0;
        }

        public bool IsGrounded()
        {
            RaycastHit hit;
            Vector3 origin = transform.position + groundCheckOffset;

            Debug.DrawRay(origin, -transform.up * groundCheckDistance, Color.red);
            return Physics.Raycast(origin, -transform.up, out hit, groundCheckDistance);
        }

        public void Jump()
        {
            if (IsGrounded())
            {
                carRb.AddForce(transform.up * jumpForce, ForceMode.Impulse);
            }
        }

        public void SetInputs(float turnAmount, float moveAmount)
        {
            moveInput.x = Mathf.Clamp(turnAmount, -1f, 1f);
            moveInput.y = Mathf.Clamp(moveAmount, -1f, 1f);
        }

        public float GetSpeed()
        {
            return carRb.linearVelocity.magnitude;
        }
        
        private void CapJumpHeight()
        {
            Vector3 vel = carRb.linearVelocity;
            if (vel.y > 6f)
            {
                vel.y = 6f;
                carRb.linearVelocity = vel;
            }
        }
    }
}
