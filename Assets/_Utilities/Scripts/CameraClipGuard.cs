using UnityEngine;

namespace _Cars.Scripts
{
    /// <summary>
    /// Fixes the gray skybox caused by the camera clipping inside meshes (benches etc.)
    /// Also enforces a minimum Y position so the camera never clips into the ground.
    /// Add to the ShakePivot GameObject where the Camera component lives.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class CameraClipGuard : MonoBehaviour
    {
        [Header("Near Clip Settings")]
        [Tooltip("Normal near clip plane — used when camera is clear of geometry")]
        [SerializeField] private float normalNearClip = 0.15f;

        [Tooltip("Near clip used when camera is inside geometry — push this up until gray goes away")]
        [SerializeField] private float insideGeometryNearClip = 1.5f;

        [Tooltip("How fast the near clip transitions")]
        [SerializeField] private float transitionSpeed = 15f;

        [Header("Detection")]
        [Tooltip("Radius of the overlap check — how close geometry needs to be")]
        [SerializeField] private float overlapRadius = 0.3f;

        [Tooltip("Layers to check — should include Ground, Default, anything physical. Exclude Players.")]
        [SerializeField] private LayerMask detectionMask;

        [Header("Ground Floor")]
        [Tooltip("Camera will never go below this world Y position — prevents ground clipping")]
        [SerializeField] private float minimumCameraY = 0.5f;
        [Tooltip("Enable the minimum Y floor")]
        [SerializeField] private bool  enforceMinimumY = true;

        private Camera     cam;
        private Collider[] overlapBuffer = new Collider[8];

        // ═══════════════════════════════════════════════
        //  LIFECYCLE
        // ═══════════════════════════════════════════════

        private void Awake()
        {
            cam               = GetComponent<Camera>();
            cam.nearClipPlane = normalNearClip;
        }

        private void LateUpdate()
        {
            // ── Minimum Y floor ───────────────────────
            if (enforceMinimumY)
            {
                Vector3 pos = transform.position;
                if (pos.y < minimumCameraY)
                {
                    pos.y              = minimumCameraY;
                    transform.position = pos;
                }
            }

            // ── Near clip geometry check ──────────────
            int count = Physics.OverlapSphereNonAlloc(
                transform.position,
                overlapRadius,
                overlapBuffer,
                detectionMask,
                QueryTriggerInteraction.Ignore);

            bool  insideGeometry = count > 0;
            float targetClip     = insideGeometry ? insideGeometryNearClip : normalNearClip;

            cam.nearClipPlane = Mathf.Lerp(
                cam.nearClipPlane,
                targetClip,
                transitionSpeed * Time.deltaTime);
        }

        // ═══════════════════════════════════════════════
        //  GIZMOS
        // ═══════════════════════════════════════════════

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, overlapRadius);

            if (enforceMinimumY)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(
                    new Vector3(transform.position.x - 2f, minimumCameraY, transform.position.z),
                    new Vector3(transform.position.x + 2f, minimumCameraY, transform.position.z));
            }
        }
    }
}