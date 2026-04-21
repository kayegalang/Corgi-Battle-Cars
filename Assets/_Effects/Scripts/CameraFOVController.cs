using UnityEngine;

public class CameraFOVController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera cam;

    [Tooltip("Reference to the player's Rigidbody")]
    [SerializeField] private Rigidbody rb;

    [Header("FOV Settings")]
    [Tooltip("The minimum speed in which FOV starts being applied (x), and speed in which the FOV application is capped (y)")]
    [SerializeField] private Vector2 fovThresholdSpeedRange = new Vector2(10f, 30f);

    [Tooltip("Maximum FOV increase at top speed")]
    [SerializeField] private float maxFOVIncrease = 15f;

    [Tooltip("How quickly the FOV smoothly transitions")]
    [SerializeField] private float smoothTime = 0.3f;

    private float baseFOV;
    private float currentFOV;
    private float fovVelocity;

    private void Awake()
    {
        if (cam == null)
            Debug.LogError("[CameraFOVController] Virtual Camera not assigned! Drag it in the Inspector.");

        if (rb == null)
            Debug.LogError("[CameraFOVController] Rigidbody not assigned! Drag it in the Inspector.");

        if (cam != null)
        {
            baseFOV = cam.fieldOfView;
            currentFOV = baseFOV;
        }
    }

    private void Update()
    {
        if (cam == null || rb == null) return;

        float speed = rb.linearVelocity.magnitude;
        float targetFOV = baseFOV;

        if (speed > fovThresholdSpeedRange.x)
        {
            // Map speed to 0-1 range within the threshold
            float speedFactor = Mathf.Clamp01(
                (speed - fovThresholdSpeedRange.x) /
                (fovThresholdSpeedRange.y - fovThresholdSpeedRange.x)
            );

            targetFOV = baseFOV + (speedFactor * maxFOVIncrease);
        }

        // SmoothDamp to the target FOV
        currentFOV = Mathf.SmoothDamp(currentFOV, targetFOV, ref fovVelocity, smoothTime);

        // Apply the FOV
        cam.fieldOfView = currentFOV;
    }
}