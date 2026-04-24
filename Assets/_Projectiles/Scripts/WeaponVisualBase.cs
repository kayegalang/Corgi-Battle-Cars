using UnityEngine;

namespace _Cars.Scripts
{
    /// <summary>
    /// Abstract base class for all weapon visuals.
    /// Add a subclass to each weapon prefab.
    ///
    /// Configure rotation axes in the Inspector per weapon:
    ///   bArK-47  → Horizontal Axis: Z
    ///   The Hound → Horizontal Axis: Y
    /// </summary>
    public abstract class WeaponVisualBase : MonoBehaviour
    {
        [Header("Turret Rotation — shared by all weapons")]
        [SerializeField] [Tooltip("The transform that rotates to face the aim direction")]
        protected Transform turretRoot;

        [SerializeField] private float rotationSpeed   = 15f;
        [SerializeField] private bool  instantRotation = false;
        [SerializeField] [Range(0f, 90f)] private float maxTiltAngle = 5f;

        [Header("Rotation Axes")]
        [Tooltip("Which axis handles horizontal 360 rotation — Z for bArK-47, Y for The Hound")]
        [SerializeField] private RotationAxis horizontalAxis = RotationAxis.Z;
        [Tooltip("Flip the horizontal rotation direction")]
        [SerializeField] private bool flipHorizontal = false;
        [Tooltip("Flip the vertical tilt direction")]
        [SerializeField] private bool flipVertical   = false;

        public enum RotationAxis { X, Y, Z }

        private Transform playerRoot;
        private bool      wasFiring = false;

        // ═══════════════════════════════════════════════
        //  SETUP
        // ═══════════════════════════════════════════════

        public void SetPlayerRoot(Transform root)
        {
            playerRoot = root;
        }

        // ═══════════════════════════════════════════════
        //  TICK — called by TurretVisuals every frame
        // ═══════════════════════════════════════════════

        public void Tick(Vector3 aimDirection, bool isFiring)
        {
            RotateTurret(aimDirection);

            if (isFiring  && !wasFiring) OnFireStart();
            if (!isFiring &&  wasFiring) OnFireStop();
            wasFiring = isFiring;

            if (isFiring) OnFiringTick();
            else          OnIdleTick();
        }

        // ═══════════════════════════════════════════════
        //  TURRET ROTATION
        // ═══════════════════════════════════════════════

        private void RotateTurret(Vector3 aimDir)
        {
            if (turretRoot == null || aimDir == Vector3.zero) return;
            if (playerRoot == null) return;

            Vector3 flatAimDir = new Vector3(aimDir.x, 0f, aimDir.z).normalized;
            if (flatAimDir == Vector3.zero) return;

            // Horizontal 360 rotation
            float hAngle = Vector3.SignedAngle(playerRoot.forward, flatAimDir, playerRoot.up);
            if (flipHorizontal) hAngle = -hAngle;

            // Vertical tilt
            float rawTilt = -Vector3.SignedAngle(flatAimDir, aimDir,
                                Vector3.Cross(flatAimDir, playerRoot.up));
            float vAngle  = Mathf.Clamp(rawTilt, -maxTiltAngle, maxTiltAngle);
            if (flipVertical) vAngle = -vAngle;

            // Build rotation based on which axis handles horizontal rotation
            Quaternion targetRotation;
            switch (horizontalAxis)
            {
                case RotationAxis.Y:
                    // The Hound: Y = horizontal, X = tilt
                    targetRotation = Quaternion.Euler(vAngle, hAngle, 0f);
                    break;
                case RotationAxis.X:
                    targetRotation = Quaternion.Euler(hAngle, 0f, vAngle);
                    break;
                default: // Z
                    // bArK-47: Z = horizontal, X = tilt
                    targetRotation = Quaternion.Euler(vAngle, 0f, hAngle);
                    break;
            }

            if (instantRotation)
                turretRoot.localRotation = targetRotation;
            else
                turretRoot.localRotation = Quaternion.Lerp(
                    turretRoot.localRotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        // ═══════════════════════════════════════════════
        //  OVERRIDE IN SUBCLASSES
        // ═══════════════════════════════════════════════

        protected virtual void OnFireStart()  { }
        protected virtual void OnFireStop()   { }
        protected virtual void OnFiringTick() { }
        protected virtual void OnIdleTick()   { }
    }
}