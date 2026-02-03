using UnityEngine;

namespace _Utilities.Scripts
{
    public class CameraFacing : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private bool lockYAxis = true;
        [SerializeField] private float minRotationDistance = 0.001f;
        [SerializeField] private float cameraSearchInterval = 0.5f;
        
        private Camera targetCamera;
        private int targetLayer = -1;
        private float cameraSearchTimer = 0f;
        
        public void SetTargetCamera(Camera camera)
        {
            targetCamera = camera;
            if (camera != null)
            {
                targetLayer = camera.gameObject.layer;
            }
        }
        
        void LateUpdate()
        {
            if (targetCamera == null && targetLayer >= 0)
            {
                TryFindCamera();
            }
    
            if (targetCamera != null)
            {
        
                FaceCamera();
            }
        }
        
        private void FaceCamera()
        {
            Vector3 directionToCamera = targetCamera.transform.position - transform.position;
            
            if (lockYAxis)
            {
                directionToCamera.y = 0; // Keep upright
            }
            
            if (directionToCamera.sqrMagnitude > minRotationDistance)
            {
                transform.rotation = Quaternion.LookRotation(directionToCamera);
            }
        }
        
        private void TryFindCamera()
        {
            cameraSearchTimer += Time.deltaTime;
            
            if (cameraSearchTimer >= cameraSearchInterval)
            {
                cameraSearchTimer = 0f;
                targetCamera = CameraUtility.FindCameraOnLayer(targetLayer);
            }
        }
    }
}