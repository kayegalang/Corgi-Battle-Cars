using UnityEngine;

namespace _Cars.Scripts
{
    /// <summary>
    /// Handles drift visual effects:
    ///   1. Exaggerated cartoon body tilt based on drift direction
    ///   2. Dust particle systems at rear wheels during drift
    /// Add to the player prefab root.
    /// Assign the car body mesh root and rear wheel dust particle systems.
    /// CarController calls SetDrifting() each FixedUpdate.
    /// </summary>
    public class DriftEffects : MonoBehaviour
    {
        [Header("Body Tilt")]
        [Tooltip("The visual mesh root of the car — this gets tilted, NOT the physics root")]
        [SerializeField] private Transform carBodyRoot;

        [Tooltip("Max Z-axis tilt angle in degrees — exaggerated cartoon feel")]
        [SerializeField] private float maxTiltAngle = 25f;

        [Tooltip("How fast the body tilts into the drift")]
        [SerializeField] private float tiltSpeed = 8f;

        [Tooltip("How fast the body returns to upright after drift ends")]
        [SerializeField] private float returnSpeed = 5f;

        [Header("Dust Particles")]
        [Tooltip("Particle system at rear-left wheel")]
        [SerializeField] private ParticleSystem dustLeft;

        [Tooltip("Particle system at rear-right wheel")]
        [SerializeField] private ParticleSystem dustRight;

        [Tooltip("Emission rate while drifting")]
        [SerializeField] private float dustEmissionRate = 25f;

        // Runtime state
        private float   currentTiltAngle = 0f;
        private float   targetTiltAngle  = 0f;
        private bool    isDrifting       = false;
        private float   driftDirection   = 0f; // -1 left, +1 right

        // ── Public getters for WheelVisuals ──────────────
        public bool  IsDrifting       => isDrifting;
        public float CurrentTiltAngle => currentTiltAngle;
        public float MaxTiltAngle     => maxTiltAngle;
        public float DriftDirection   => driftDirection;

        // ═══════════════════════════════════════════════
        //  LIFECYCLE
        // ═══════════════════════════════════════════════

        private void Awake()
        {
            StopDust();
        }

        private void Update()
        {
            UpdateTilt();
        }

        // ═══════════════════════════════════════════════
        //  PUBLIC API — called from CarController
        // ═══════════════════════════════════════════════

        /// <summary>
        /// Call from CarController every FixedUpdate with current drift state.
        /// driftDir: negative = drifting left, positive = drifting right, 0 = not drifting
        /// </summary>
        public void SetDrifting(bool drifting, float driftDir)
        {
            if (drifting && !isDrifting)
            {
                // Capture direction ONCE when drift starts — never changes mid-drift
                driftDirection = Mathf.Sign(driftDir);
            }
            else if (!drifting)
            {
                driftDirection = 0f;
            }

            isDrifting = drifting;
            driftDirection  = driftDir;

            if (drifting)
            {
                // Rear swings opposite to turn direction — classic drift sliding look
                targetTiltAngle = driftDir * maxTiltAngle;
                PlayDust();
            }
            else
            {
                targetTiltAngle = 0f;
                StopDust();
            }
        }

        // ═══════════════════════════════════════════════
        //  TILT
        // ═══════════════════════════════════════════════

        private void UpdateTilt()
        {
            if (carBodyRoot == null) return;

            float speed = isDrifting ? tiltSpeed : returnSpeed;
            currentTiltAngle = Mathf.Lerp(currentTiltAngle, targetTiltAngle, speed * Time.deltaTime);

            // Apply on Y axis — car body visually rotates like rear is sliding out
            // X and Z stay 0 so it doesn't fight with physics rotation
            carBodyRoot.localEulerAngles = new Vector3(0f, currentTiltAngle, 0f);
        }

        // ═══════════════════════════════════════════════
        //  DUST
        // ═══════════════════════════════════════════════

        private void PlayDust()
        {
            SetDustEmission(dustLeft,  true);
            SetDustEmission(dustRight, true);
        }

        private void StopDust()
        {
            SetDustEmission(dustLeft,  false);
            SetDustEmission(dustRight, false);
        }

        private void SetDustEmission(ParticleSystem ps, bool active)
        {
            if (ps == null) return;

            var emission = ps.emission;

            if (active)
            {
                emission.enabled          = true;
                emission.rateOverTime     = dustEmissionRate;
                if (!ps.isPlaying) ps.Play();
            }
            else
            {
                emission.enabled = false;
                ps.Stop();
            }
        }
    }
}