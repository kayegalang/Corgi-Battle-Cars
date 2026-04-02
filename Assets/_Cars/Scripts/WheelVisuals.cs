using UnityEngine;

namespace _Cars.Scripts
{
    /// <summary>
    /// Handles visual wheel rotation:
    ///   1. All wheels spin based on car speed
    ///   2. Front wheels steer with input
    /// Drift rotation is handled automatically since wheels
    /// are children of CarVisual which DriftEffects rotates.
    /// Add to the player prefab root.
    /// </summary>
    public class WheelVisuals : MonoBehaviour
    {
        [Header("Wheel Meshes — assign the visual child transforms")]
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

        // Track spin per wheel
        private float spinFL = 0f;
        private float spinFR = 0f;
        private float spinRL = 0f;
        private float spinRR = 0f;

        private float currentSteerAngle = 0f;

        // ═══════════════════════════════════════════════
        //  LIFECYCLE
        // ═══════════════════════════════════════════════

        private void Awake()
        {
            carRb         = GetComponent<Rigidbody>();
            carController = GetComponent<CarController>();
        }

        private void Update()
        {
            if (carRb == null) return;

            UpdateSpin();
            UpdateSteer();
            ApplyWheelRotations();
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
            float steerInput  = carController != null ? carController.GetMoveInput().x : 0f;
            float targetSteer = steerInput * maxSteerAngle;
            currentSteerAngle = Mathf.Lerp(currentSteerAngle, targetSteer, steerSpeed * Time.deltaTime);
        }

        // ═══════════════════════════════════════════════
        //  APPLY
        // ═══════════════════════════════════════════════

        private void ApplyWheelRotations()
        {
            // Front wheels steer, rear wheels stay straight
            // Left and right need opposite Y to point same direction
            ApplyRotation(wheelFL, spinFL,  currentSteerAngle);
            ApplyRotation(wheelFR, spinFR, -currentSteerAngle);
            ApplyRotation(wheelRL, spinRL,  0f);
            ApplyRotation(wheelRR, spinRR,  0f);
        }

        private void ApplyRotation(Transform wheel, float spinAngle, float steerAngle)
        {
            if (wheel == null) return;
            wheel.localRotation = Quaternion.Euler(spinAngle, steerAngle, 0f);
        }
    }
}