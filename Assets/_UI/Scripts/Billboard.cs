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

        // ═══════════════════════════════════════════════
        //  LIFECYCLE
        // ═══════════════════════════════════════════════

        private void LateUpdate()
        {
            Camera cam = FindNearestCamera();
            if (cam == null) return;

            FaceCamera(cam);
        }

        // ═══════════════════════════════════════════════
        //  FACING
        // ═══════════════════════════════════════════════

        private void FaceCamera(Camera cam)
        {
            Vector3 directionToCamera = transform.position - cam.transform.position;

            if (directionToCamera == Vector3.zero) return;

            if (lockXAxis)
            {
                // Only rotate around Y axis — tag stays upright
                directionToCamera.y = 0f;
            }

            if (directionToCamera == Vector3.zero) return;

            transform.rotation = Quaternion.LookRotation(directionToCamera);
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
