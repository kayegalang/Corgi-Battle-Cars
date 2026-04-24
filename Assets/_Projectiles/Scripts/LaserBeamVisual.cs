using UnityEngine;

namespace _Cars.Scripts
{
    /// <summary>
    /// Attach to the LaserBeam GameObject (child of FirePoint) inside The Hound weapon prefab.
    /// Uses CarShooter.GetAimDirection() so the laser follows the reticle correctly.
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    public class LaserBeamVisual : MonoBehaviour
    {
        [Header("Laser Settings")]
        [SerializeField] private float maxRange = 50f;
        [SerializeField] private LayerMask hitMask;
        [SerializeField] private float beamWidth = 0.05f;

        [Header("Pulse Settings")]
        [SerializeField] private bool  pulseWidth  = true;
        [SerializeField] private float pulseSpeed  = 8f;
        [SerializeField] private float pulseAmount = 0.02f;

        [Header("Impact Effect")]
        [SerializeField] private ParticleSystem impactEffect;

        private GameObject owner;
        private CarShooter carShooter;
        private int        damagePerSecond = 20;
        private float      damageInterval  = 0.1f;
        private float      nextDamageTime  = 0f;
        private LineRenderer lineRenderer;
        private bool         isActive = false;

        // ═══════════════════════════════════════════════
        //  LIFECYCLE
        // ═══════════════════════════════════════════════

        private void Awake()
        {
            lineRenderer = GetComponent<LineRenderer>();
            lineRenderer.positionCount = 2;
            lineRenderer.useWorldSpace = true;
            SetWidth(beamWidth);
        }

        private void Update()
        {
            if (!isActive) return;

            UpdateBeam();

            if (pulseWidth)
            {
                float pulse = beamWidth + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;
                SetWidth(pulse);
            }
        }

        // ═══════════════════════════════════════════════
        //  SETUP — called by HoundVisual
        // ═══════════════════════════════════════════════

        public void SetDamageSource(GameObject ownerObject, CarShooter shooter, int dps, float interval)
        {
            owner           = ownerObject;
            carShooter      = shooter;
            damagePerSecond = dps;
            damageInterval  = interval;
        }

        public void SetActive(bool active)
        {
            isActive = active;
            gameObject.SetActive(active);

            if (!active && impactEffect != null && impactEffect.isPlaying)
                impactEffect.Stop();
        }

        // ═══════════════════════════════════════════════
        //  BEAM UPDATE + DAMAGE
        // ═══════════════════════════════════════════════

        private void UpdateBeam()
        {
            Vector3 startPos  = transform.position;

            // Use CarShooter aim direction so laser follows the reticle
            Vector3 direction = carShooter != null
                ? carShooter.GetAimDirection()
                : transform.forward;

            if (direction == Vector3.zero) direction = transform.forward;

            if (Physics.Raycast(startPos, direction, out RaycastHit hit, maxRange, hitMask))
            {
                lineRenderer.SetPosition(0, startPos);
                lineRenderer.SetPosition(1, hit.point);

                if (impactEffect != null)
                {
                    impactEffect.transform.position = hit.point;
                    impactEffect.transform.rotation = Quaternion.LookRotation(-direction);
                    if (!impactEffect.isPlaying) impactEffect.Play();
                }

                // Deal damage at intervals
                if (Time.time >= nextDamageTime)
                {
                    nextDamageTime = Time.time + damageInterval;

                    CarHealth health = hit.collider.GetComponent<CarHealth>()
                        ?? hit.collider.transform.root.GetComponent<CarHealth>();

                    // Don't damage the owner — check both collider and its root
                    bool isOwner = owner != null &&
                        (hit.collider.gameObject == owner ||
                         hit.collider.transform.root.gameObject == owner);

                    if (health != null && !isOwner)
                    {
                        int damage = Mathf.RoundToInt(damagePerSecond * damageInterval);
                        health.TakeDamage(damage, owner);
                    }
                }
            }
            else
            {
                lineRenderer.SetPosition(0, startPos);
                lineRenderer.SetPosition(1, startPos + direction * maxRange);

                if (impactEffect != null && impactEffect.isPlaying)
                    impactEffect.Stop();
            }
        }

        // ═══════════════════════════════════════════════
        //  HELPERS
        // ═══════════════════════════════════════════════

        private void SetWidth(float width)
        {
            lineRenderer.startWidth = width;
            lineRenderer.endWidth   = width * 0.5f;
        }
    }
}