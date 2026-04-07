using System.Collections.Generic;
using _PowerUps.Scripts;
using UnityEngine;

namespace _Gameplay.Scripts
{
    /// <summary>
    /// Cinematic free camera for trailer recording.
    ///
    /// ═══ MODES ═══════════════════════════════════════
    ///
    /// FREE CAM (default)
    ///   WASD          Move
    ///   Q / E         Down / Up
    ///   Mouse         Look
    ///   Scroll        Adjust speed
    ///   Left Shift    Fast move
    ///
    /// FOLLOW MODE  (press F2)
    ///   Locks onto a car and smoothly tracks it.
    ///   F2            Cycle to next target
    ///   Scroll        Adjust follow distance
    ///   Mouse         Orbit around target
    ///   Hold Alt      Temporarily unlock to reframe
    ///
    /// DOLLY MODE  (press F3)
    ///   Camera moves along a fixed path at a set speed.
    ///   F3            Toggle dolly playback on/off
    ///   P             Add a waypoint at the current camera position/rotation
    ///   Backspace     Remove the last waypoint
    ///   Scroll        Adjust dolly speed
    ///   Hold Space    Pause dolly mid-path
    ///   R             Reset to start of path
    ///
    /// GLOBAL
    ///   F1            Toggle cinematic cam on/off
    ///                 PlayerOne is hidden while cinematic cam is active.
    ///                 Power-ups and smell trails are always visible while active.
    ///
    /// ═════════════════════════════════════════════════
    /// </summary>
    public class CinematicFreeCam : MonoBehaviour
    {
        // ═══════════════════════════════════════════════
        //  INSPECTOR
        // ═══════════════════════════════════════════════

        [Header("Global")]
        [SerializeField] private KeyCode toggleKey = KeyCode.F1;
        [SerializeField] private KeyCode followKey = KeyCode.F2;
        [SerializeField] private KeyCode dollyKey  = KeyCode.F3;

        [Header("Free Cam — Movement")]
        [SerializeField] private float moveSpeed       = 10f;
        [SerializeField] private float fastMultiplier  = 3f;
        [SerializeField] private float scrollSpeedStep = 2f;
        [SerializeField] private float minSpeed        = 1f;
        [SerializeField] private float maxSpeed        = 50f;

        [Header("Free Cam — Look")]
        [SerializeField] private float lookSensitivity = 2f;
        [SerializeField] private float smoothTime      = 0.05f;

        [Header("Follow Mode")]
        [SerializeField] private float followDistance    = 8f;
        [SerializeField] private float followHeight      = 3f;
        [SerializeField] private float followSmoothSpeed = 5f;
        [SerializeField] private float orbitSensitivity  = 2f;
        [SerializeField] private float minFollowDistance = 2f;
        [SerializeField] private float maxFollowDistance = 30f;

        [Header("Dolly Mode")]
        [SerializeField] private float dollySpeed     = 3f;
        [SerializeField] private float minDollySpeed  = 0.5f;
        [SerializeField] private float maxDollySpeed  = 20f;
        [SerializeField] private float dollyRotSmooth = 3f;
        [SerializeField] private bool  dollyLoop      = false;

        // ═══════════════════════════════════════════════
        //  STATE
        // ═══════════════════════════════════════════════

        private enum Mode { Free, Follow, Dolly }
        private Mode currentMode = Mode.Free;

        private Camera cinemaCam;
        private bool   isActive = false;

        // Free cam
        private float   yaw, pitch;
        private Vector3 smoothVel   = Vector3.zero;
        private Vector3 currentMove = Vector3.zero;

        // Follow mode
        private List<Transform> followTargets = new List<Transform>();
        private int             followIndex   = 0;
        private float           orbitYaw      = 0f;
        private float           orbitPitch    = 20f;

        // Dolly mode
        private struct Waypoint { public Vector3 position; public Quaternion rotation; }
        private List<Waypoint> waypoints     = new List<Waypoint>();
        private float          dollyProgress = 0f;
        private bool           dollyPlaying  = false;

        // ═══════════════════════════════════════════════
        //  LIFECYCLE
        // ═══════════════════════════════════════════════

        private void Awake()
        {
            cinemaCam = GetComponent<Camera>();
            if (cinemaCam == null)
            {
                Debug.LogError("[CinematicFreeCam] No Camera component found!");
                return;
            }

            yaw   = transform.eulerAngles.y;
            pitch = transform.eulerAngles.x;

            cinemaCam.enabled = false;
        }

        private void Update()
        {
            HandleGlobalToggle();
            if (!isActive) return;

            HandleModeSwitch();

            switch (currentMode)
            {
                case Mode.Free:   UpdateFreeCam();    break;
                case Mode.Follow: UpdateFollowMode(); break;
                case Mode.Dolly:  UpdateDollyMode();  break;
            }
        }

        // ═══════════════════════════════════════════════
        //  GLOBAL TOGGLE
        // ═══════════════════════════════════════════════

        private void HandleGlobalToggle()
        {
            if (!Input.GetKeyDown(toggleKey)) return;

            isActive          = !isActive;
            cinemaCam.enabled = isActive;

            GameObject player = GameObject.FindWithTag("PlayerOne");

            if (isActive)
            {
                yaw   = transform.eulerAngles.y;
                pitch = transform.eulerAngles.x;
                LockCursor();
                RefreshFollowTargets();

                if (player != null) player.SetActive(false);
                SetPowerUpsAlwaysVisible(true);

                Debug.Log("[CinematicFreeCam] ON  |  F1=off  F2=follow  F3=dolly");
            }
            else
            {
                dollyPlaying = false;
                UnlockCursor();

                if (player != null) player.SetActive(true);
                SetPowerUpsAlwaysVisible(false);

                Debug.Log("[CinematicFreeCam] OFF");
            }
        }

        // ═══════════════════════════════════════════════
        //  POWER-UP VISIBILITY
        // ═══════════════════════════════════════════════

        private void SetPowerUpsAlwaysVisible(bool alwaysVisible)
        {
            foreach (var pickup in FindObjectsByType<PowerUpPickup>(FindObjectsSortMode.None))
                pickup.SetCinematicVisible(alwaysVisible);
        }

        // ═══════════════════════════════════════════════
        //  MODE SWITCHING
        // ═══════════════════════════════════════════════

        private void HandleModeSwitch()
        {
            if (Input.GetKeyDown(followKey))
            {
                if (currentMode == Mode.Follow)
                    CycleFollowTarget();
                else
                    EnterFollowMode();
            }

            if (Input.GetKeyDown(dollyKey))
            {
                if (currentMode == Mode.Dolly)
                    ExitDollyMode();
                else
                    EnterDollyMode();
            }
        }

        // ═══════════════════════════════════════════════
        //  FREE CAM
        // ═══════════════════════════════════════════════

        private void UpdateFreeCam()
        {
            HandleFreeLook();
            HandleFreeMove();
            HandleFreeScrollSpeed();
        }

        private void HandleFreeLook()
        {
            yaw   += Input.GetAxis("Mouse X") * lookSensitivity;
            pitch -= Input.GetAxis("Mouse Y") * lookSensitivity;
            pitch  = Mathf.Clamp(pitch, -89f, 89f);
            transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
        }

        private void HandleFreeMove()
        {
            float speed = moveSpeed * (Input.GetKey(KeyCode.LeftShift) ? fastMultiplier : 1f);

            Vector3 input = new Vector3(
                Input.GetAxisRaw("Horizontal"),
                (Input.GetKey(KeyCode.E) ? 1f : 0f) - (Input.GetKey(KeyCode.Q) ? 1f : 0f),
                Input.GetAxisRaw("Vertical")
            );

            Vector3 target = transform.TransformDirection(input.normalized) * speed;
            currentMove = Vector3.SmoothDamp(currentMove, target, ref smoothVel, smoothTime);
            transform.position += currentMove * Time.deltaTime;
        }

        private void HandleFreeScrollSpeed()
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) < 0.01f) return;
            moveSpeed = Mathf.Clamp(moveSpeed + scroll * scrollSpeedStep * 10f, minSpeed, maxSpeed);
            Debug.Log($"[CinematicFreeCam] Speed: {moveSpeed:F1}");
        }

        // ═══════════════════════════════════════════════
        //  FOLLOW MODE
        // ═══════════════════════════════════════════════

        private void EnterFollowMode()
        {
            RefreshFollowTargets();
            if (followTargets.Count == 0)
            {
                Debug.LogWarning("[CinematicFreeCam] No cars found to follow!");
                return;
            }

            currentMode = Mode.Follow;
            followIndex = 0;
            InitOrbitFromCurrentAngle();
            Debug.Log($"[CinematicFreeCam] FOLLOW MODE — tracking {followTargets[followIndex].name}  |  F2=next target");
        }

        private void CycleFollowTarget()
        {
            if (followTargets.Count == 0) return;
            followIndex = (followIndex + 1) % followTargets.Count;
            InitOrbitFromCurrentAngle();
            Debug.Log($"[CinematicFreeCam] Now following: {followTargets[followIndex].name}");
        }

        private void InitOrbitFromCurrentAngle()
        {
            if (followTargets.Count == 0) return;
            Vector3 offset = transform.position - followTargets[followIndex].position;
            orbitYaw   = Mathf.Atan2(offset.x, offset.z) * Mathf.Rad2Deg;
            orbitPitch = Mathf.Clamp(
                Mathf.Atan2(offset.y, new Vector2(offset.x, offset.z).magnitude) * Mathf.Rad2Deg,
                -30f, 80f);
        }

        private void UpdateFollowMode()
        {
            followTargets.RemoveAll(t => t == null);
            if (followTargets.Count == 0) { currentMode = Mode.Free; return; }
            followIndex = Mathf.Clamp(followIndex, 0, followTargets.Count - 1);

            Transform target = followTargets[followIndex];

            bool altHeld = Input.GetKey(KeyCode.LeftAlt);
            if (!altHeld)
            {
                orbitYaw   += Input.GetAxis("Mouse X") * orbitSensitivity;
                orbitPitch -= Input.GetAxis("Mouse Y") * orbitSensitivity;
                orbitPitch  = Mathf.Clamp(orbitPitch, -20f, 80f);
            }

            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.01f)
                followDistance = Mathf.Clamp(followDistance - scroll * 10f, minFollowDistance, maxFollowDistance);

            Quaternion orbitRot   = Quaternion.Euler(orbitPitch, orbitYaw, 0f);
            Vector3    desiredPos = target.position + orbitRot * new Vector3(0f, followHeight, -followDistance);

            transform.position = Vector3.Lerp(transform.position, desiredPos, followSmoothSpeed * Time.deltaTime);
            transform.LookAt(target.position + Vector3.up * followHeight * 0.5f);
        }

        private void RefreshFollowTargets()
        {
            followTargets.Clear();

            string[] tags = { "PlayerOne", "PlayerTwo", "PlayerThree", "PlayerFour",
                               "BotOne",    "BotTwo",    "BotThree",    "BotFour" };

            foreach (string tag in tags)
            {
                GameObject go = GameObject.FindWithTag(tag);
                if (go != null) followTargets.Add(go.transform);
            }

            Debug.Log($"[CinematicFreeCam] Found {followTargets.Count} follow targets.");
        }

        // ═══════════════════════════════════════════════
        //  DOLLY MODE
        // ═══════════════════════════════════════════════

        private void EnterDollyMode()
        {
            currentMode   = Mode.Dolly;
            dollyPlaying  = false;
            dollyProgress = 0f;
            Debug.Log("[CinematicFreeCam] DOLLY MODE  |  P=add waypoint  Backspace=remove last  F3=play/stop  R=reset  Scroll=speed  Space=pause");
        }

        private void ExitDollyMode()
        {
            dollyPlaying = false;
            currentMode  = Mode.Free;
            yaw   = transform.eulerAngles.y;
            pitch = transform.eulerAngles.x;
            Debug.Log("[CinematicFreeCam] FREE CAM MODE");
        }

        private void UpdateDollyMode()
        {
            if (Input.GetKeyDown(KeyCode.P))
            {
                waypoints.Add(new Waypoint { position = transform.position, rotation = transform.rotation });
                Debug.Log($"[CinematicFreeCam] Waypoint {waypoints.Count} added.");
            }

            if (Input.GetKeyDown(KeyCode.Backspace) && waypoints.Count > 0)
            {
                waypoints.RemoveAt(waypoints.Count - 1);
                Debug.Log($"[CinematicFreeCam] Waypoint removed. Remaining: {waypoints.Count}");
            }

            if (Input.GetKeyDown(dollyKey))
            {
                if (waypoints.Count < 2)
                {
                    Debug.LogWarning("[CinematicFreeCam] Need at least 2 waypoints to play dolly!");
                    return;
                }
                dollyPlaying = !dollyPlaying;
                Debug.Log(dollyPlaying ? "[CinematicFreeCam] Dolly PLAYING" : "[CinematicFreeCam] Dolly PAUSED");
            }

            if (Input.GetKeyDown(KeyCode.R))
            {
                dollyProgress = 0f;
                dollyPlaying  = false;
                if (waypoints.Count > 0)
                    transform.SetPositionAndRotation(waypoints[0].position, waypoints[0].rotation);
                Debug.Log("[CinematicFreeCam] Dolly RESET");
            }

            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.01f)
            {
                dollySpeed = Mathf.Clamp(dollySpeed + scroll * 5f, minDollySpeed, maxDollySpeed);
                Debug.Log($"[CinematicFreeCam] Dolly speed: {dollySpeed:F1}");
            }

            bool paused = Input.GetKey(KeyCode.Space);
            if (dollyPlaying && !paused && waypoints.Count >= 2)
                AdvanceDolly();

            if (!dollyPlaying)
            {
                HandleFreeLook();
                HandleFreeMove();
            }

            DrawDollyPath();
        }

        private void AdvanceDolly()
        {
            float totalSegments = waypoints.Count - 1;

            dollyProgress += (dollySpeed * Time.deltaTime) / GetApproxPathLength();
            dollyProgress  = dollyLoop ? dollyProgress % 1f : Mathf.Clamp01(dollyProgress);

            float scaled   = dollyProgress * totalSegments;
            int   segIndex = Mathf.Min(Mathf.FloorToInt(scaled), waypoints.Count - 2);
            float segT     = scaled - segIndex;

            Vector3    pos       = CatmullRomPosition(segIndex, segT);
            Quaternion targetRot = Quaternion.Slerp(waypoints[segIndex].rotation, waypoints[segIndex + 1].rotation, segT);
            Quaternion smoothRot = Quaternion.Slerp(transform.rotation, targetRot, dollyRotSmooth * Time.deltaTime * 10f);

            transform.position = pos;
            transform.rotation = smoothRot;

            if (!dollyLoop && dollyProgress >= 1f)
            {
                dollyPlaying = false;
                Debug.Log("[CinematicFreeCam] Dolly path complete.");
            }
        }

        private Vector3 CatmullRomPosition(int segIndex, float t)
        {
            Vector3 p0 = waypoints[Mathf.Max(segIndex - 1, 0)].position;
            Vector3 p1 = waypoints[segIndex].position;
            Vector3 p2 = waypoints[Mathf.Min(segIndex + 1, waypoints.Count - 1)].position;
            Vector3 p3 = waypoints[Mathf.Min(segIndex + 2, waypoints.Count - 1)].position;

            float t2 = t * t;
            float t3 = t2 * t;

            return 0.5f * (
                2f * p1 +
                (-p0 + p2) * t +
                (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 +
                (-p0 + 3f * p1 - 3f * p2 + p3) * t3
            );
        }

        private float GetApproxPathLength()
        {
            float length = 0f;
            for (int i = 0; i < waypoints.Count - 1; i++)
                length += Vector3.Distance(waypoints[i].position, waypoints[i + 1].position);
            return Mathf.Max(length, 0.1f);
        }

        private void DrawDollyPath()
        {
            if (waypoints.Count < 2) return;
            for (int i = 0; i < waypoints.Count - 1; i++)
                Debug.DrawLine(waypoints[i].position, waypoints[i + 1].position, Color.cyan);
        }

        // ═══════════════════════════════════════════════
        //  CURSOR HELPERS
        // ═══════════════════════════════════════════════

        private void LockCursor()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible   = false;
        }

        private void UnlockCursor()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible   = true;
        }
    }
}