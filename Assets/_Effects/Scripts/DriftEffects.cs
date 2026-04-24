using UnityEngine;

namespace _Cars.Scripts
{
    public class DriftEffects : MonoBehaviour
    {
        [Header("Body Tilt")]
        [Tooltip("The visual mesh root of the car — this gets tilted, NOT the physics root")]
        [SerializeField] private Transform carBodyRoot;
        [SerializeField] private float maxTiltAngle = 25f;
        [SerializeField] private float tiltSpeed    = 8f;
        [SerializeField] private float returnSpeed  = 5f;

        [Header("Dust Particles")]
        [SerializeField] private ParticleSystem dustLeft;
        [SerializeField] private ParticleSystem dustRight;
        [SerializeField] private float dustEmissionRate = 25f;

        private float currentTiltAngle = 0f;
        private float targetTiltAngle  = 0f;
        private bool  isDrifting       = false;
        private float driftDirection   = 0f;

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
        //  REWIRING — called by CarVisualLoader after spawn
        // ═══════════════════════════════════════════════

        private Vector3 baseEulerAngles = Vector3.zero;

        public void SetReferences(Transform bodyRoot, ParticleSystem dLeft, ParticleSystem dRight)
        {
            carBodyRoot = bodyRoot;
            dustLeft    = dLeft;
            dustRight   = dRight;

            // Cache the base rotation of the car model so drift tilt doesn't override it
            if (carBodyRoot != null)
                baseEulerAngles = carBodyRoot.localEulerAngles;

            Debug.Log("[DriftEffects] References rewired from spawned car prefab.");
        }

        // ═══════════════════════════════════════════════
        //  PUBLIC API
        // ═══════════════════════════════════════════════

        public void SetDrifting(bool drifting, float driftDir)
        {
            if (drifting && !isDrifting)
                driftDirection = Mathf.Sign(driftDir);
            else if (!drifting)
                driftDirection = 0f;

            isDrifting     = drifting;
            driftDirection = driftDir;

            if (drifting)
            {
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

            float speed      = isDrifting ? tiltSpeed : returnSpeed;
            currentTiltAngle = Mathf.Lerp(currentTiltAngle, targetTiltAngle, speed * Time.deltaTime);

            // Apply Y tilt on top of the car's base rotation — preserves baked-in X rotation (e.g. -90)
            carBodyRoot.localEulerAngles = new Vector3(
                baseEulerAngles.x,
                baseEulerAngles.y + currentTiltAngle,
                baseEulerAngles.z);
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
                emission.enabled      = true;
                emission.rateOverTime = dustEmissionRate;
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