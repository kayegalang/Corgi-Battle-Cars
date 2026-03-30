using System.Collections.Generic;
using UnityEngine;
using Unity.Cinemachine;
using _UI.Scripts;
using _Cars.Scripts;
using _Player.Scripts;
using _PowerUps.Scripts;

namespace _Utilities.Scripts
{
    /// <summary>
    /// Standalone free roam camera for capturing cinematic trailer footage.
    /// Add to a new Camera GameObject in the scene (not on the player prefab).
    ///
    /// Disables ONLY the Camera + CinemachineBrain components on PlayerOne's cameras
    /// so PlayerOne can still be controlled by a controller while you fly the cinematic cam.
    /// Switches PlayerOne to Gamepad-only input so WASD doesn't drive the car.
    ///
    /// Hierarchy expected:
    ///   PlayerOne
    ///     -- PlayerCamera       (has Camera + CinemachineBrain)
    ///     -- CinemachineCamera  (has CinemachineCamera component)
    ///
    /// Controls:
    ///   F1               = toggle cinematic mode on/off
    ///   H                = toggle HUD on/off (hidden by default for clean recordings)
    ///   WASD             = move forward/back/left/right
    ///   Q / E            = move down / up
    ///   Right-click drag = look around
    ///   Shift            = cycle speed: Normal → Fast → Slow → Normal
    ///   Scroll wheel     = adjust base move speed
    ///   F                = toggle slow motion
    ///   R                = reset to original position
    /// </summary>
    public class CinematicFreeCam : MonoBehaviour
    {
        [Header("Settings")]
        [Tooltip("Name of PlayerOne's Camera GameObject (has Camera + CinemachineBrain)")]
        [SerializeField] private string playerCameraName      = "PlayerCamera";

        [Tooltip("Name of PlayerOne's CinemachineCamera GameObject (sibling of PlayerCamera)")]
        [SerializeField] private string cinemachineCameraName = "CinemachineCamera";

        [Tooltip("Key to toggle cinematic mode on/off")]
        [SerializeField] private KeyCode toggleKey = KeyCode.F1;

        [Header("Movement")]
        [SerializeField] private float moveSpeed      = 10f;
        [SerializeField] private float fastMultiplier = 3f;
        [SerializeField] private float slowMultiplier = 0.25f;

        [Header("Look")]
        [SerializeField] private float mouseSensitivity = 2f;
        [SerializeField] private float smoothing        = 2f;

        [Header("Slow Motion")]
        [SerializeField] private float slowMotionScale = 0.3f;

        private enum SpeedMode { Normal, Fast, Slow }

        private Camera            cam;
        private Camera            playerCam;
        private CinemachineBrain  playerBrain;
        private CinemachineCamera cinemachineCam;

        private Vector3    originalPosition;
        private Quaternion originalRotation;
        private Vector2    currentMouseDelta;
        private float      yaw;
        private float      pitch;
        private bool       isSlowMotion    = false;
        private bool       isCinematicMode = false;
        private bool       showHUD         = false;
        private SpeedMode  speedMode       = SpeedMode.Normal;

        // UI components to hide/show
        private List<PlayerUIManager> playerUIs   = new List<PlayerUIManager>();
        private List<PowerUpHandler>  powerUpUIs  = new List<PowerUpHandler>();
        private List<CarShooter>      carShooters = new List<CarShooter>();

        // ═══════════════════════════════════════════════
        //  LIFECYCLE
        // ═══════════════════════════════════════════════

        private void Awake()
        {
            cam = GetComponent<Camera>();

            originalPosition = transform.position;
            originalRotation = transform.rotation;

            yaw   = transform.eulerAngles.y;
            pitch = transform.eulerAngles.x;

            if (cam != null)
            {
                cam.cullingMask = ~0;
                cam.enabled     = false;
            }
        }

        private void Start()
        {
            // PlayerOne spawns at runtime — we always retry on toggle anyway
            FindPlayerCameraComponents();
        }

        private void FindPlayerCameraComponents()
        {
            // Find PlayerCamera — has Camera + CinemachineBrain
            GameObject playerCameraGO = GameObject.Find(playerCameraName);
            if (playerCameraGO != null)
            {
                playerCam   = playerCameraGO.GetComponent<Camera>();
                playerBrain = playerCameraGO.GetComponent<CinemachineBrain>();
                Debug.Log($"[CinematicFreeCam] Found {playerCameraName} — " +
                          $"Camera: {playerCam != null}, Brain: {playerBrain != null}");
            }
            else
            {
                Debug.LogWarning($"[CinematicFreeCam] Could not find '{playerCameraName}'!");
            }

            // Find CinemachineCamera sibling — has CinemachineCamera component
            GameObject cinemachineGO = GameObject.Find(cinemachineCameraName);
            if (cinemachineGO != null)
            {
                cinemachineCam = cinemachineGO.GetComponent<CinemachineCamera>();
                Debug.Log($"[CinematicFreeCam] Found {cinemachineCameraName} — " +
                          $"CinemachineCamera: {cinemachineCam != null}");
            }
            else
            {
                Debug.LogWarning($"[CinematicFreeCam] Could not find '{cinemachineCameraName}'!");
            }
        }

        private void FindAllPlayerUI()
        {
            playerUIs   = new List<PlayerUIManager>(FindObjectsByType<PlayerUIManager>(FindObjectsSortMode.None));
            powerUpUIs  = new List<PowerUpHandler>(FindObjectsByType<PowerUpHandler>(FindObjectsSortMode.None));
            carShooters = new List<CarShooter>(FindObjectsByType<CarShooter>(FindObjectsSortMode.None));
        }

        private void Update()
        {
            if (Input.GetKeyDown(toggleKey))
                ToggleCinematicMode();

            if (Input.GetKeyDown(KeyCode.H))
                showHUD = !showHUD;

            if (!isCinematicMode) return;

            HandleLook();
            HandleMovement();
            HandleSpeedCycle();
            HandleSlowMotion();
            HandleReset();
            HandleSpeedScroll();
        }

        // ═══════════════════════════════════════════════
        //  TOGGLE
        // ═══════════════════════════════════════════════

        private void ToggleCinematicMode()
        {
            isCinematicMode = !isCinematicMode;

            if (isCinematicMode)
                EnableCinematicMode();
            else
                DisableCinematicMode();
        }

        private void EnableCinematicMode()
        {
            // Always re-find since PlayerOne spawns at runtime after Start()
            FindPlayerCameraComponents();

            // Disable ONLY the Camera and CinemachineBrain components —
            // PlayerOne's GameObject stays active so controller input still works!
            if (playerCam      != null) playerCam.enabled      = false;
            if (playerBrain    != null) playerBrain.enabled    = false;
            if (cinemachineCam != null) cinemachineCam.enabled = false;

            // Enable our cinematic camera
            if (cam != null) cam.enabled = true;

            // Allow all devices so friend's controller can drive PlayerOne
            SetAllDevices(true);

            // Find all player UI and car components
            FindAllPlayerUI();
            HideAllPlayerUI();

            // Lock PlayerOne to gamepad only — keyboard drives the cinematic cam, not the car
            SetCarControllersGamepadOnly(true);

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible   = true;

            yaw       = transform.eulerAngles.y;
            pitch     = transform.eulerAngles.x;
            speedMode = SpeedMode.Normal;

            Debug.Log("[CinematicFreeCam] 🎬 Cinematic mode ON — friend can drive with controller! [H] for controls");
        }

        private void DisableCinematicMode()
        {
            // Re-enable PlayerOne's camera components
            if (playerCam      != null) playerCam.enabled      = true;
            if (playerBrain    != null) playerBrain.enabled    = true;
            if (cinemachineCam != null) cinemachineCam.enabled = true;

            // Disable our cinematic camera
            if (cam != null) cam.enabled = false;

            // Restore device restrictions
            SetAllDevices(false);

            // Restore all player UI
            ShowAllPlayerUI();

            // Restore keyboard+mouse control scheme for PlayerOne
            SetCarControllersGamepadOnly(false);

            if (isSlowMotion)
            {
                isSlowMotion   = false;
                Time.timeScale = 1f;
            }

            speedMode = SpeedMode.Normal;

            Debug.Log("[CinematicFreeCam] 🎬 Cinematic mode OFF — PlayerOne camera restored.");
        }

        private void OnDisable()
        {
            if (isCinematicMode)
                DisableCinematicMode();
        }

        // ═══════════════════════════════════════════════
        //  DEVICE HELPER
        // ═══════════════════════════════════════════════

        private void SetAllDevices(bool allow)
        {
            var guards = FindObjectsByType<PlayerOneUIGuard>(FindObjectsSortMode.None);
            if (guards.Length > 0)
                guards[0].SetAllowAllDevices(allow);
            // No warning — PlayerOneUIGuard won't exist in DesignerTuning
        }

        private void SetCarControllersGamepadOnly(bool gamepadOnly)
        {
            foreach (var shooter in carShooters)
            {
                if (shooter == null) continue;
                var controller = shooter.GetComponent<CarController>();
                controller?.SetGamepadOnly(gamepadOnly);
            }
        }

        // ═══════════════════════════════════════════════
        //  UI HIDE / SHOW
        //  Disables Canvas components rather than GameObjects
        //  to avoid disabling the PlayerOne car itself!
        // ═══════════════════════════════════════════════

        private void HideAllPlayerUI()
        {
            foreach (var ui in playerUIs)
            {
                if (ui == null) continue;
                // Disable the Canvas — NOT the GameObject (which would disable the whole car!)
                var canvas = ui.GetComponentInChildren<Canvas>();
                if (canvas != null) canvas.enabled = false;
                else                ui.enabled     = false;
            }

            foreach (var handler in powerUpUIs)
                if (handler != null) handler.OnGameEnd();

            foreach (var shooter in carShooters)
                if (shooter != null) shooter.DisableReticle();
        }

        private void ShowAllPlayerUI()
        {
            foreach (var ui in playerUIs)
            {
                if (ui == null) continue;
                var canvas = ui.GetComponentInChildren<Canvas>();
                if (canvas != null) canvas.enabled = true;
                else                ui.enabled     = true;
            }

            foreach (var shooter in carShooters)
                if (shooter != null) shooter.EnableGameplay();
        }

        // ═══════════════════════════════════════════════
        //  LOOK
        // ═══════════════════════════════════════════════

        private void HandleLook()
        {
            if (!Input.GetMouseButton(1))
            {
                Cursor.lockState = CursorLockMode.None;
                return;
            }

            Cursor.lockState = CursorLockMode.Locked;

            float mouseX = Input.GetAxisRaw("Mouse X") * mouseSensitivity;
            float mouseY = Input.GetAxisRaw("Mouse Y") * mouseSensitivity;

            currentMouseDelta = Vector2.Lerp(currentMouseDelta,
                new Vector2(mouseX, mouseY), 1f / smoothing);

            yaw   += currentMouseDelta.x;
            pitch -= currentMouseDelta.y;
            pitch  = Mathf.Clamp(pitch, -89f, 89f);

            transform.eulerAngles = new Vector3(pitch, yaw, 0f);
        }

        // ═══════════════════════════════════════════════
        //  MOVEMENT
        // ═══════════════════════════════════════════════

        private void HandleMovement()
        {
            float speed = moveSpeed;

            switch (speedMode)
            {
                case SpeedMode.Fast: speed *= fastMultiplier; break;
                case SpeedMode.Slow: speed *= slowMultiplier; break;
            }

            Vector3 move = Vector3.zero;

            if (Input.GetKey(KeyCode.W)) move += transform.forward;
            if (Input.GetKey(KeyCode.S)) move -= transform.forward;
            if (Input.GetKey(KeyCode.A)) move -= transform.right;
            if (Input.GetKey(KeyCode.D)) move += transform.right;
            if (Input.GetKey(KeyCode.E)) move += Vector3.up;
            if (Input.GetKey(KeyCode.Q)) move -= Vector3.up;

            transform.position += move * speed * Time.unscaledDeltaTime;
        }

        // ═══════════════════════════════════════════════
        //  SPEED CYCLE
        // ═══════════════════════════════════════════════

        private void HandleSpeedCycle()
        {
            if (!Input.GetKeyDown(KeyCode.LeftShift)) return;

            speedMode = speedMode switch
            {
                SpeedMode.Normal => SpeedMode.Fast,
                SpeedMode.Fast   => SpeedMode.Slow,
                SpeedMode.Slow   => SpeedMode.Normal,
                _                => SpeedMode.Normal
            };

            Debug.Log($"[CinematicFreeCam] Speed: {speedMode}");
        }

        // ═══════════════════════════════════════════════
        //  SLOW MOTION
        // ═══════════════════════════════════════════════

        private void HandleSlowMotion()
        {
            if (!Input.GetKeyDown(KeyCode.F)) return;

            isSlowMotion   = !isSlowMotion;
            Time.timeScale = isSlowMotion ? slowMotionScale : 1f;
            Debug.Log($"[CinematicFreeCam] Slow motion: {isSlowMotion}");
        }

        // ═══════════════════════════════════════════════
        //  RESET
        // ═══════════════════════════════════════════════

        private void HandleReset()
        {
            if (!Input.GetKeyDown(KeyCode.R)) return;

            transform.position = originalPosition;
            transform.rotation = originalRotation;
            yaw   = originalRotation.eulerAngles.y;
            pitch = originalRotation.eulerAngles.x;
            Debug.Log("[CinematicFreeCam] Reset!");
        }

        // ═══════════════════════════════════════════════
        //  SPEED SCROLL
        // ═══════════════════════════════════════════════

        private void HandleSpeedScroll()
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) < 0.001f) return;

            moveSpeed = Mathf.Clamp(moveSpeed + scroll * 10f, 1f, 100f);
        }
    }
}