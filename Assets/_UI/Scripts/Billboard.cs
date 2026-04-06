using UnityEngine;

namespace _UI.Scripts
{
    /// <summary>
    /// Makes a World Space UI element always face the nearest active camera.
    /// Add to the NameTagCanvas GameObject so the collar always faces the viewer.
    /// </summary>
    public class Billboard : MonoBehaviour
    {
        [Tooltip("If true, only rotates on the Y axis — good for name tags that should stay upright")]
        [SerializeField] private bool lockXAxis = true;

        [Tooltip("The Transform the name tag should follow (usually the car's license plate or root)")]
        [SerializeField] private Transform followTarget;

        // ═══════════════════════════════════════════════
        //  LIFECYCLE
        // ═══════════════════════════════════════════════

        private void LateUpdate()
        {
            if (followTarget != null)
            {
                // Make the UI follow the car's position
                transform.position = followTarget.position;
            }

            Camera cam = FindNearestCamera();
            if (cam == null) return;

            FaceCamera(cam);
        }

        // ═══════════════════════════════════════════════
        //  FACING
        // ═══════════════════════════════════════════════

        private void FaceCamera(Camera cam)
        {
            Vector3 directionToCamera = cam.transform.position - transform.position;

            if (directionToCamera == Vector3.zero) return;

            // Look at camera while keeping car's up direction
            transform.rotation = Quaternion.LookRotation(directionToCamera, followTarget.up);
        }

        // ═══════════════════════════════════════════════
        //  FIND CAMERA
        // ═══════════════════════════════════════════════

        private Camera FindNearestCamera()
        {
            Camera nearest  = null;
            float  minDist  = Mathf.Infinity;

            foreach (Camera cam in Camera.allCameras)
            {
                if (!cam.isActiveAndEnabled) continue;

                float dist = Vector3.Distance(transform.position, cam.transform.position);
                if (dist < minDist)
                {
                    minDist = dist;
                    nearest = cam;
                }
            }

            return nearest;
        }
    }
}
