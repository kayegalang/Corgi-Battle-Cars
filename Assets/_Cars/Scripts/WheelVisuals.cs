using UnityEngine;
using _Bot.Scripts;

namespace _Cars.Scripts
{
    public class WheelVisuals : MonoBehaviour
    {
        [Header("Wheel Visual Meshes — assign the MESH child, NOT the collider object")]
        [SerializeField] private Transform wheelFL;
        [SerializeField] private Transform wheelFR;
        [SerializeField] private Transform wheelRL;
        [SerializeField] private Transform wheelRR;

        [Header("Spin Settings")]
        [SerializeField] private float spinSpeed = 200f;

        [Header("Steering Settings")]
        [SerializeField] private float maxSteerAngle = 30f;
        [SerializeField] private float steerSpeed    = 8f;

        private Rigidbody     carRb;
        private CarController carController;
        private BotController botController;

        private float spinFL, spinFR, spinRL, spinRR;
        private float currentSteerAngle;

        // ═══════════════════════════════════════════════
        //  LIFECYCLE
        // ═══════════════════════════════════════════════

        private void Awake()
        {
            carRb         = GetComponent<Rigidbody>();
            carController = GetComponent<CarController>();
            botController = GetComponent<BotController>();

            if (carController == null && botController == null)
                Debug.LogError($"[WheelVisuals] No CarController or BotController found on {gameObject.name}!");
        }

        private void Update()
        {
            if (carRb == null) return;

            UpdateSpin();
            UpdateSteer();
            ApplyWheelRotations();
        }

        // ═══════════════════════════════════════════════
        //  REWIRING — called by CarVisualLoader after spawn
        // ═══════════════════════════════════════════════

        public void SetWheels(Transform fl, Transform fr, Transform rl, Transform rr)
        {
            wheelFL = fl;
            wheelFR = fr;
            wheelRL = rl;
            wheelRR = rr;
            Debug.Log("[WheelVisuals] Wheels rewired from spawned car prefab.");
        }

        // ═══════════════════════════════════════════════
        //  SPIN
        // ═══════════════════════════════════════════════

        private void UpdateSpin()
        {
            float forwardSpeed = transform.InverseTransformDirection(carRb.linearVelocity).z;
            float spinDelta    = forwardSpeed * spinSpeed * Time.deltaTime;

            spinFL += spinDelta;
            spinFR += spinDelta;
            spinRL += spinDelta;
            spinRR += spinDelta;
        }

        // ═══════════════════════════════════════════════
        //  STEER
        // ═══════════════════════════════════════════════

        private void UpdateSteer()
        {
            float steerInput = 0f;
            if (carController != null)      steerInput = carController.GetMoveInput().x;
            else if (botController != null) steerInput = botController.GetMoveInput().x;

            float targetSteer = steerInput * maxSteerAngle;
            currentSteerAngle = Mathf.Lerp(currentSteerAngle, targetSteer, steerSpeed * Time.deltaTime);
        }

        // ═══════════════════════════════════════════════
        //  APPLY
        // ═══════════════════════════════════════════════

        private void ApplyWheelRotations()
        {
            ApplyRotation(wheelFL, spinFL, currentSteerAngle);
            ApplyRotation(wheelFR, spinFR, currentSteerAngle);
            ApplyRotation(wheelRL, spinRL, 0f);
            ApplyRotation(wheelRR, spinRR, 0f);
        }

        private void ApplyRotation(Transform wheel, float spinAngle, float steerAngle)
        {
            if (wheel == null) return;
            wheel.localRotation = Quaternion.Euler(spinAngle, steerAngle, 0f);
        }
    }
}