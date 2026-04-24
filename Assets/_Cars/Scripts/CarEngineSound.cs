using UnityEngine;
using FMOD.Studio;
using FMODUnity;
using _Audio.scripts;

namespace _Cars.Scripts
{
    /// <summary>
    /// Plays the engine rev sound while the car is moving, stops when it stops.
    /// Add to DefaultPlayer root.
    /// </summary>
    public class CarEngineSound : MonoBehaviour
    {
        [Header("Settings")]
        [Tooltip("Minimum speed before engine sound plays")]
        [SerializeField] private float minSpeedThreshold = 0.5f;

        private EventInstance engineInstance;
        private Rigidbody     carRb;
        private bool          isPlaying       = false;
        private bool          instanceCreated = false;

        // ═══════════════════════════════════════════════
        //  LIFECYCLE
        // ═══════════════════════════════════════════════

        private void Start()
        {
            carRb = GetComponent<Rigidbody>();

            // Only play engine sound in gameplay scenes
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "MainMenu")
                return;

            CreateInstance();
        }

        private void Update()
        {
            if (!instanceCreated || carRb == null) return;

            float speed   = carRb.linearVelocity.magnitude;
            bool  moving  = speed > minSpeedThreshold;

            if (moving && !isPlaying)
            {
                engineInstance.start();
                isPlaying = true;
            }
            else if (!moving && isPlaying)
            {
                engineInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
                isPlaying = false;
            }

            // Keep 3D position updated
            engineInstance.set3DAttributes(RuntimeUtils.To3DAttributes(transform));
        }

        private void OnDestroy() => Cleanup();
        private void OnDisable() => Cleanup();

        private void OnEnable()
        {
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "MainMenu")
                return;

            if (!instanceCreated) CreateInstance();
        }

        // ═══════════════════════════════════════════════
        //  INSTANCE
        // ═══════════════════════════════════════════════

        private void CreateInstance()
        {
            if (FMODEvents.instance == null) return;

            engineInstance  = RuntimeManager.CreateInstance(FMODEvents.instance.carengine);
            instanceCreated = true;
            RuntimeManager.AttachInstanceToGameObject(engineInstance, transform);
        }

        private void Cleanup()
        {
            if (!instanceCreated) return;
            engineInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
            engineInstance.release();
            instanceCreated = false;
            isPlaying       = false;
        }
    }
}