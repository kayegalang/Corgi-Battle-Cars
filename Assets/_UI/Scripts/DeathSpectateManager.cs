using System;
using System.Collections.Generic;
using _Cars.Scripts;
using TMPro;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace _UI.Scripts
{
    public class DeathSpectateManager : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject       deathScreenPanel;
        [SerializeField] private TextMeshProUGUI  respawnCountdownText;
        [SerializeField] private TextMeshProUGUI  spectatingText;
        [SerializeField] private Button           previousPlayerButton;
        [SerializeField] private Button           nextPlayerButton;

        [Header("UI Text")]
        [SerializeField] private string respawnCountdownFormat  = "Respawning in {0}";
        [SerializeField] private string spectatingFormat        = "Spectating: {0}";
        [SerializeField] private string noPlayersToSpectateText = "No players to spectate";

        private Camera            playerCamera;
        private CinemachineBrain  cinemachineBrain;
        private PlayerInput       playerInput;
        private string            playerTag;
        private bool              isDead = false;

        private float  respawnTimer    = 0f;
        private float  respawnDuration = 3f;
        private Action onRespawnCallback;

        private List<GameObject> alivePlayersCache   = new List<GameObject>();
        private int              currentSpectateIndex = 0;

        // Camera bookmarks — restored on respawn
        private Transform  originalCameraParent;
        private Vector3    originalCameraLocalPosition;
        private Quaternion originalCameraLocalRotation;
        private Rect       originalViewportRect;

        // ── FIX: Track spectate target by Transform reference only.
        // We NEVER call SetParent on the spectated player's hierarchy.
        // If that player gets destroyed, our camera is safe in its own hierarchy.
        // LateUpdate copies their camera's world position/rotation each frame instead.
        private Transform spectateFollowTarget = null;
        private bool      isSpectatingBot      = false; // bots have no Camera child

        private const float ALIVE_CHECK_INTERVAL = 0.5f;
        private float aliveCheckTimer = 0f;

        // ═══════════════════════════════════════════════
        //  STARTUP
        // ═══════════════════════════════════════════════

        private void Awake()
        {
            HideDeathScreen();
            SetupButtons();
        }

        private void Start()
        {
            InitializeCamera();
            ConstrainPanelToViewport();
        }

        private void InitializeCamera()
        {
            playerCamera     = GetComponentInChildren<Camera>();
            cinemachineBrain = GetComponentInChildren<CinemachineBrain>();
            playerInput      = GetComponent<PlayerInput>();

            if (playerCamera == null)
            {
                Debug.LogError($"[{nameof(DeathSpectateManager)}] No camera found on {gameObject.name}!");
                return;
            }

            originalViewportRect = playerCamera.rect;
            originalCameraParent = playerCamera.transform.parent;

            // Camera is now on ShakePivot (child of PlayerCamera).
            // For spectating bots we need the offset from the car root —
            // use CinemachineBrain (PlayerCamera) position which has the real offset.
            Transform offsetSource = cinemachineBrain != null
                ? cinemachineBrain.transform
                : playerCamera.transform;

            originalCameraLocalPosition = offsetSource.localPosition;
            originalCameraLocalRotation = offsetSource.localRotation;
        }

        private void ConstrainPanelToViewport()
        {
            if (deathScreenPanel == null || playerCamera == null) return;

            RectTransform panelRect = deathScreenPanel.GetComponent<RectTransform>();
            if (panelRect == null) return;

            panelRect.anchorMin = new Vector2(originalViewportRect.x,    originalViewportRect.y);
            panelRect.anchorMax = new Vector2(originalViewportRect.xMax, originalViewportRect.yMax);
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;
        }

        private void SetupButtons()
        {
            if (previousPlayerButton != null)
                previousPlayerButton.onClick.AddListener(SpectatePreviousPlayer);

            if (nextPlayerButton != null)
                nextPlayerButton.onClick.AddListener(SpectateNextPlayer);
        }

        // ═══════════════════════════════════════════════
        //  UPDATE
        // ═══════════════════════════════════════════════

        private bool spectateInputConsumed = false;

        private void Update()
        {
            if (!isDead) return;

            UpdateRespawnTimer();
            CheckAlivePlayersCache();
            HandleSpectateInput();
        }

        private void HandleSpectateInput()
        {
            if (alivePlayersCache.Count <= 1) return;

            Gamepad pad = null;
            if (playerInput != null)
            {
                foreach (var device in playerInput.devices)
                    if (device is Gamepad gp) { pad = gp; break; }
            }
            if (pad == null) pad = Gamepad.current;
            if (pad == null) return;

            bool leftPressed  = pad.leftStick.x.ReadValue() < -0.5f || pad.dpad.left.isPressed;
            bool rightPressed = pad.leftStick.x.ReadValue() >  0.5f || pad.dpad.right.isPressed;

            if (!leftPressed && !rightPressed)
            {
                spectateInputConsumed = false;
                return;
            }

            if (spectateInputConsumed) return;
            spectateInputConsumed = true;

            if (leftPressed) SpectatePreviousPlayer();
            else             SpectateNextPlayer();
        }

        private void LateUpdate()
        {
            if (!isDead || playerCamera == null) return;

            // Always lock viewport rect to our split-screen slice
            playerCamera.rect = originalViewportRect;

            // Follow the spectate target by copying world position/rotation —
            // NO SetParent so our camera can never be destroyed by the target dying
            if (spectateFollowTarget != null)
            {
                if (isSpectatingBot)
                {
                    // Bot: follow bot root with original camera offset
                    playerCamera.transform.position = spectateFollowTarget.TransformPoint(originalCameraLocalPosition);
                    playerCamera.transform.rotation = spectateFollowTarget.rotation * originalCameraLocalRotation;
                }
                else
                {
                    // Human: mirror their camera's world transform exactly
                    playerCamera.transform.position = spectateFollowTarget.position;
                    playerCamera.transform.rotation = spectateFollowTarget.rotation;
                }
            }

            if (IsKeyboardPlayer())
                ConfineCursorToViewport();
        }

        private bool IsKeyboardPlayer() =>
            playerInput != null && playerInput.currentControlScheme == "Keyboard";

        private void ConfineCursorToViewport()
        {
            if (Mouse.current == null) return;

            float  minX     = originalViewportRect.x    * Screen.width;
            float  maxX     = originalViewportRect.xMax * Screen.width;
            float  minY     = originalViewportRect.y    * Screen.height;
            float  maxY     = originalViewportRect.yMax * Screen.height;
            Vector2 mousePos = Mouse.current.position.ReadValue();
            Vector2 clamped  = new Vector2(
                Mathf.Clamp(mousePos.x, minX, maxX),
                Mathf.Clamp(mousePos.y, minY, maxY));

            if (clamped != mousePos)
                Mouse.current.WarpCursorPosition(clamped);
        }

        // ═══════════════════════════════════════════════
        //  DEATH ENTRY POINT
        // ═══════════════════════════════════════════════

        public void OnPlayerDeath(string tag, float respawnDelay, Action onRespawn)
        {
            playerTag        = tag;
            respawnDuration  = respawnDelay;
            respawnTimer     = respawnDuration;
            onRespawnCallback = onRespawn;
            isDead           = true;

            ShowDeathScreen();
            DisablePlayerControls();

            if (IsKeyboardPlayer())
            {
                Cursor.visible   = true;
                Cursor.lockState = CursorLockMode.None;
            }

            // Stop Cinemachine fighting us while we manually position the camera
            if (cinemachineBrain != null)
                cinemachineBrain.enabled = false;

            FindAlivePlayersToSpectate();
            StartSpectating();
        }

        private void ShowDeathScreen()
        {
            if (deathScreenPanel != null)
                deathScreenPanel.SetActive(true);

            UpdateRespawnCountdownDisplay();
        }

        private void HideDeathScreen()
        {
            if (deathScreenPanel != null)
                deathScreenPanel.SetActive(false);
        }

        private void DisablePlayerControls()
        {
            var carController = GetComponent<CarController>();
            if (carController != null) carController.enabled = false;

            var carShooter = GetComponent<CarShooter>();
            if (carShooter != null) carShooter.enabled = false;
        }

        // ═══════════════════════════════════════════════
        //  SPECTATING
        // ═══════════════════════════════════════════════

        private void FindAlivePlayersToSpectate()
        {
            alivePlayersCache.Clear();

            foreach (CarHealth car in FindObjectsByType<CarHealth>(FindObjectsSortMode.None))
            {
                if (car.gameObject.CompareTag(playerTag)) continue;
                if (car.IsDead()) continue;
                alivePlayersCache.Add(car.gameObject);
            }

            Debug.Log($"[{nameof(DeathSpectateManager)}] {playerTag} found {alivePlayersCache.Count} targets");
        }

        private void StartSpectating()
        {
            if (alivePlayersCache.Count == 0)
            {
                // ── FIX: All players dead — park camera in place, show message
                spectateFollowTarget = null;
                UpdateSpectatingDisplay(noPlayersToSpectateText);
                UpdateNavigationButtons(false);
                Debug.Log($"[{nameof(DeathSpectateManager)}] {playerTag}: no alive players — camera parked in place.");
                return;
            }

            currentSpectateIndex = 0;
            SpectatePlayer(currentSpectateIndex);
            UpdateNavigationButtons(true);
        }

        private void SpectatePlayer(int index)
        {
            if (index < 0 || index >= alivePlayersCache.Count) return;

            GameObject target = alivePlayersCache[index];

            if (target == null)
            {
                FindAlivePlayersToSpectate();
                StartSpectating();
                return;
            }

            SetSpectateTarget(target);
            UpdateSpectatingDisplay(target.tag);
        }

        private void SetSpectateTarget(GameObject target)
        {
            // Try to find a Camera child (human players have one, bots don't)
            Camera targetCam = target.GetComponentInChildren<Camera>();

            if (targetCam != null)
            {
                // Human: follow their camera's Transform world position in LateUpdate
                spectateFollowTarget = targetCam.transform;
                isSpectatingBot      = false;
            }
            else
            {
                // Bot: follow the bot root and use our original local offset
                spectateFollowTarget = target.transform;
                isSpectatingBot      = true;
            }
        }

        private void CheckAlivePlayersCache()
        {
            aliveCheckTimer += Time.deltaTime;
            if (aliveCheckTimer < ALIVE_CHECK_INTERVAL) return;
            aliveCheckTimer = 0f;

            // If spectate target was destroyed or died, refresh
            bool targetGone = spectateFollowTarget == null
                || alivePlayersCache.Count == 0
                || currentSpectateIndex >= alivePlayersCache.Count
                || alivePlayersCache[currentSpectateIndex] == null;

            if (targetGone)
            {
                FindAlivePlayersToSpectate();
                StartSpectating();
            }
        }

        private void SpectatePreviousPlayer()
        {
            if (alivePlayersCache.Count == 0) return;

            currentSpectateIndex--;
            if (currentSpectateIndex < 0)
                currentSpectateIndex = alivePlayersCache.Count - 1;

            SpectatePlayer(currentSpectateIndex);
        }

        private void SpectateNextPlayer()
        {
            if (alivePlayersCache.Count == 0) return;

            currentSpectateIndex++;
            if (currentSpectateIndex >= alivePlayersCache.Count)
                currentSpectateIndex = 0;

            SpectatePlayer(currentSpectateIndex);
        }

        private void UpdateSpectatingDisplay(string displayText)
        {
            if (spectatingText != null)
                spectatingText.text = string.Format(spectatingFormat, displayText);
        }

        private void UpdateNavigationButtons(bool interactable)
        {
            bool moreThanOne = alivePlayersCache.Count > 1;

            if (previousPlayerButton != null)
                previousPlayerButton.interactable = interactable && moreThanOne;

            if (nextPlayerButton != null)
                nextPlayerButton.interactable = interactable && moreThanOne;
        }

        // ═══════════════════════════════════════════════
        //  TIMER
        // ═══════════════════════════════════════════════

        private void UpdateRespawnTimer()
        {
            respawnTimer -= Time.deltaTime;
            UpdateRespawnCountdownDisplay();

            if (respawnTimer <= 0f)
                OnRespawn();
        }

        private void UpdateRespawnCountdownDisplay()
        {
            if (respawnCountdownText == null) return;

            int seconds = Mathf.CeilToInt(respawnTimer);
            respawnCountdownText.text = string.Format(respawnCountdownFormat, seconds);
        }

        // ═══════════════════════════════════════════════
        //  RESPAWN
        // ═══════════════════════════════════════════════

        private void OnRespawn()
        {
            isDead               = false;
            spectateFollowTarget = null;

            HideDeathScreen();
            RestoreCamera();

            onRespawnCallback?.Invoke();
            onRespawnCallback = null;
        }

        public void ForceHide()
        {
            if (!isDead) return;

            isDead               = false;
            spectateFollowTarget = null;
            StopAllCoroutines();
            HideDeathScreen();
        }

        private void RestoreCamera()
        {
            if (playerCamera == null || originalCameraParent == null) return;

            // Camera was never reparented, so just reset local transform and re-enable Cinemachine
            playerCamera.transform.SetParent(originalCameraParent);
            playerCamera.transform.localPosition = originalCameraLocalPosition;
            playerCamera.transform.localRotation = originalCameraLocalRotation;
            playerCamera.rect                    = originalViewportRect;

            if (cinemachineBrain != null)
                cinemachineBrain.enabled = true;
        }

        // ═══════════════════════════════════════════════
        //  CLEANUP
        // ═══════════════════════════════════════════════

        private void OnDestroy()
        {
            if (previousPlayerButton != null)
                previousPlayerButton.onClick.RemoveAllListeners();

            if (nextPlayerButton != null)
                nextPlayerButton.onClick.RemoveAllListeners();
        }
    }
}