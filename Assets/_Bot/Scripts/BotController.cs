using Player.Scripts;
using UnityEngine;

namespace _Bot.Scripts
{
    public class BotController : MonoBehaviour
    {
        private Vector2 moveInput; 
        private Rigidbody carRb;
        
        [SerializeField] private CarStats carStats;

        void Awake()
        {
            carRb = GetComponent<Rigidbody>();
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
            
            if (carRb.linearVelocity.magnitude > carStats.maxSpeed)
            {
                carRb.linearVelocity = carRb.linearVelocity.normalized * carStats.maxSpeed;
            }
        }

        private void Turn()
        {
            float turnInput = moveInput.x;
            
            if (IsMovingForward())
            {
                carRb.AddTorque(Vector3.up * turnInput * carStats.turnSpeed);
            }
            else
            {
                carRb.AddTorque(-Vector3.up * turnInput * carStats.turnSpeed);
            }
        }

        private void Move()
        {
            float moveValue = moveInput.y;

            if (Mathf.Abs(moveValue) > 0.01f)
            {
                carRb.AddRelativeForce(Vector3.forward * moveValue * carStats.acceleration);
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
            Vector3 origin = transform.position + carStats.groundCheckOffset;

            Debug.DrawRay(origin, -transform.up * carStats.groundCheckDistance, Color.red);
            return Physics.Raycast(origin, -transform.up, out hit, carStats.groundCheckDistance);
        }

        public void Jump()
        {
            if (IsGrounded())
            {
                carRb.AddForce(transform.up * carStats.jumpForce, ForceMode.Impulse);
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
