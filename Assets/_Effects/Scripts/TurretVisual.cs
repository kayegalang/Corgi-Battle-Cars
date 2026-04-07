using UnityEngine;
using _Bot.Scripts;

namespace _Cars.Scripts
{
    /// <summary>
    /// Rotates the turret model to face the current aim direction,
    /// and spins the barrel while firing.
    /// Automatically finds CarShooter (player) or BotAI (bot) on the root.
    /// Add to the player/bot prefab root.
    /// </summary>
    public class TurretVisuals : MonoBehaviour
    {
        [Header("Turret Model")]
        [SerializeField] private Transform turretModel;

        [Header("Barrel")]
        [SerializeField] [Tooltip("The barrel transform that spins on Y when firing")]
        private Transform barrelModel;
        [SerializeField] [Tooltip("Degrees per second the barrel spins while firing")]
        private float barrelSpinSpeed = 720f;
        [SerializeField] [Tooltip("How fast the barrel decelerates when not firing")]
        private float barrelDeceleration = 360f;

        [Header("Rotation Settings")]
        [SerializeField] private float rotationSpeed = 15f;
        [SerializeField] private bool instantRotation = false;

        [Header("Tilt Constraints")]
        [SerializeField] [Range(0f, 90f)] [Tooltip("Max degrees the turret can tilt up/down")]
        private float maxTiltAngle = 5f;

        private CarShooter carShooter;
        private BotAI      botAI;

        private float currentBarrelSpin   = 0f;
        private float currentSpinVelocity = 0f;

        // ═══════════════════════════════════════════════
        //  LIFECYCLE
        // ═══════════════════════════════════════════════

        private void Awake()
        {
            carShooter = GetComponent<CarShooter>();
            botAI      = GetComponent<BotAI>();

            if (carShooter == null && botAI == null)
                Debug.LogError($"[TurretVisuals] No CarShooter or BotAI found on {gameObject.name}!");
        }

        private void Update()
        {
            UpdateTurretAim();
            UpdateBarrelSpin();
        }

        // ═══════════════════════════════════════════════
        //  TURRET AIM
        // ═══════════════════════════════════════════════

        private void UpdateTurretAim()
        {
            if (turretModel == null) return;

            Vector3 aimDir = GetAimDirection();
            if (aimDir == Vector3.zero) return;

            Vector3 flatAimDir = new Vector3(aimDir.x, 0f, aimDir.z).normalized;
            if (flatAimDir == Vector3.zero) return;

            float horizontalAngle = Vector3.SignedAngle(transform.forward, flatAimDir, transform.up);

            float rawTilt     = -Vector3.SignedAngle(flatAimDir, aimDir, Vector3.Cross(flatAimDir, Vector3.up));
            float clampedTilt = Mathf.Clamp(rawTilt, -maxTiltAngle, maxTiltAngle);

            Quaternion targetRotation = Quaternion.Euler(-90f + clampedTilt, 0f, horizontalAngle);

            if (instantRotation)
                turretModel.localRotation = targetRotation;
            else
                turretModel.localRotation = Quaternion.Lerp(turretModel.localRotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        // ═══════════════════════════════════════════════
        //  BARREL SPIN
        // ═══════════════════════════════════════════════

        private void UpdateBarrelSpin()
        {
            if (barrelModel == null) return;

            bool isFiring = GetIsFiring();

            if (isFiring)
                currentSpinVelocity = barrelSpinSpeed;
            else
                currentSpinVelocity = Mathf.MoveTowards(currentSpinVelocity, 0f, barrelDeceleration * Time.deltaTime);

            currentBarrelSpin += currentSpinVelocity * Time.deltaTime;
            barrelModel.localRotation = Quaternion.Euler(0f, currentBarrelSpin, 0f);
        }

        // ═══════════════════════════════════════════════
        //  HELPERS
        // ═══════════════════════════════════════════════

        private Vector3 GetAimDirection()
        {
            if (carShooter != null) return carShooter.GetAimDirection();
            if (botAI      != null) return botAI.GetAimDirection();
            return Vector3.zero;
        }

        private bool GetIsFiring()
        {
            if (carShooter != null) return carShooter.IsFiring();
            if (botAI      != null) return botAI.IsFiring();
            return false;
        }
    }
}