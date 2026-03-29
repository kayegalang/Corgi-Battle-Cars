using System.Collections.Generic;
using UnityEngine;
using _UI.Scripts;
using _Cars.Scripts;
using _PowerUps.Scripts;

namespace _Utilities.Scripts
{
    /// <summary>
    /// Standalone free roam camera for capturing cinematic trailer footage.
    /// Add to a new Camera GameObject in the scene (not on the player prefab).
    /// Dynamically finds PlayerCamera by name at runtime.
    /// Hides all player UI when cinematic mode is active.
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
        [Tooltip("Name of PlayerOne's camera GameObject to hide during cinematic mode")]
        [SerializeField] private string playerCameraName = "PlayerCamera";

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

        private Camera     cam;
        private GameObject playerCamera;
        private Vector3    originalPosition;
        private Quaternion originalRotation;
        private Vector2    currentMouseDelta;
        private float      yaw;
        private float      pitch;
        private bool       isSlowMotion    = false;
        private bool       isCinematicMode = false;
        private bool       showHUD         = false; // off by default for clean recordings
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
            FindPlayerCamera();
        }

        private void FindPlayerCamera()
        {
            GameObject found = GameObject.Find(playerCameraName);
            if (found != null)
            {
                playerCamera = found;
                Debug.Log($"[CinematicFreeCam] Found {playerCameraName}!");
            }
            else
            {
                Debug.LogWarning($"[CinematicFreeCam] Could not find '{playerCameraName}' — will retry when toggled.");
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

            // H toggles HUD visibility regardless of cinematic mode
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
            if (playerCamera == null)
                FindPlayerCamera();

            if (playerCamera != null)
                playerCamera.SetActive(false);

            if (cam != null)
                cam.enabled = true;

            FindAllPlayerUI();
            HideAllPlayerUI();

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible   = true;

            yaw       = transform.eulerAngles.y;
            pitch     = transform.eulerAngles.x;
            speedMode = SpeedMode.Normal;

            Debug.Log("[CinematicFreeCam] 🎬 Cinematic mode ON — [H] to show controls");
        }

        private void DisableCinematicMode()
        {
            if (playerCamera != null)
                playerCamera.SetActive(true);

            if (cam != null)
                cam.enabled = false;

            ShowAllPlayerUI();

            if (isSlowMotion)
            {
                isSlowMotion   = false;
                Time.timeScale = 1f;
            }

            speedMode = SpeedMode.Normal;

            Debug.Log("[CinematicFreeCam] 🎬 Cinematic mode OFF");
        }

        private void OnDisable()
        {
            if (isCinematicMode)
                DisableCinematicMode();
        }

        // ═══════════════════════════════════════════════
        //  UI HIDE / SHOW
        // ═══════════════════════════════════════════════

        private void HideAllPlayerUI()
        {
            foreach (var ui in playerUIs)
                if (ui != null) ui.gameObject.SetActive(false);

            foreach (var handler in powerUpUIs)
                if (handler != null) handler.OnGameEnd();

            foreach (var shooter in carShooters)
                if (shooter != null) shooter.DisableReticle();
        }

        private void ShowAllPlayerUI()
        {
            foreach (var ui in playerUIs)
                if (ui != null) ui.gameObject.SetActive(true);

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

        // ═══════════════════════════════════════════════
        //  HUD — press H to toggle, hidden by default
        // ═══════════════════════════════════════════════

        private void OnGUI()
        {
            // Always show a tiny hint that H toggles the HUD
            GUIStyle hintStyle = new GUIStyle();
            hintStyle.normal.textColor = new Color(0f, 1f, 1f, 0.5f);
            hintStyle.fontSize         = 12;

            if (!isCinematicMode)
            {
                GUI.Label(new Rect(10, 10, 300, 20),
                    $"[{toggleKey}] Enter Cinematic Mode", hintStyle);
                return;
            }

            if (!showHUD) return;

            // Full HUD — only when H is pressed
            GUIStyle style = new GUIStyle();
            style.normal.textColor = new Color(0f, 1f, 1f, 0.9f);
            style.fontSize         = 14;

            string speedLabel = speedMode switch
            {
                SpeedMode.Fast => "FAST ⚡",
                SpeedMode.Slow => "SLOW 🐢",
                _              => "Normal"
            };

            GUI.Label(new Rect(10, 30, 400, 200),
                $"🎬 CINEMATIC MODE  [{toggleKey}] Exit\n" +
                $"WASD = move  |  Q/E = up/down\n" +
                $"Right-click drag = look\n" +
                $"[Shift] Speed: {speedLabel}\n" +
                $"[F] Slow motion: {(isSlowMotion ? "ON 🔴" : "off")}\n" +
                $"[R] Reset position\n" +
                $"Base speed: {moveSpeed:F0}  (scroll to adjust)",
                style);
        }
    }
}