using UnityEngine;
using _Bot.Scripts;

namespace _Cars.Scripts
{
    /// <summary>
    /// Handles visual wheel rotation for both players and bots.
    /// Automatically finds CarController (player) or BotController (bot) on the root.
    /// Assign the visual mesh children, NOT the collider objects.
    /// </summary>
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
            ApplyRotation(wheelFL, spinFL,  currentSteerAngle);
            ApplyRotation(wheelFR, spinFR,  currentSteerAngle);
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