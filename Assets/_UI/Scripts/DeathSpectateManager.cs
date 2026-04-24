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
        private bool              isDead      = false;
        private bool              isSuppressed = false;

        private float  respawnTimer    = 0f;
        private float  respawnDuration = 3f;
        private Action onRespawnCallback;

        private List<GameObject> alivePlayersCache   = new List<GameObject>();
        private int              currentSpectateIndex = 0;

        private Transform  originalCameraParent;
        private Vector3    originalCameraLocalPosition;
        private Quaternion originalCameraLocalRotation;
        private Rect       originalViewportRect;

        private Transform spectateFollowTarget = null;
        private bool      isSpectatingBot      = false;

        private const float ALIVE_CHECK_INTERVAL = 0.5f;
        private float aliveCheckTimer = 0f;

        // ═══════════════════════════════════════════════
        //  SUPPRESS — called by PresentationDirector
        // ═══════════════════════════════════════════════

        /// <summary>
        /// When suppressed the death screen never shows.
        /// Used by PresentationDirector during Tab focus mode.
        /// </summary>
        public void SetSuppressed(bool suppressed)
        {
            isSuppressed = suppressed;
            if (suppressed) HideDeathScreen();
        }

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
                foreach (var device in playerInput.devices)
                    if (device is Gamepad gp) { pad = gp; break; }
            if (pad == null) pad = Gamepad.current;
            if (pad == null) return;

            bool leftPressed  = pad.leftStick.x.ReadValue() < -0.5f || pad.dpad.left.isPressed;
            bool rightPressed = pad.leftStick.x.ReadValue() >  0.5f || pad.dpad.right.isPressed;

            if (!leftPressed && !rightPressed) { spectateInputConsumed = false; return; }
            if (spectateInputConsumed) return;
            spectateInputConsumed = true;

            if (leftPressed) SpectatePreviousPlayer();
            else             SpectateNextPlayer();
        }

        private void LateUpdate()
        {
            if (!isDead || playerCamera == null) return;
            if (isSuppressed) return; // don't touch viewports in presentation mode

            playerCamera.rect = originalViewportRect;

            if (spectateFollowTarget != null)
            {
                if (isSpectatingBot)
                {
                    playerCamera.transform.position = spectateFollowTarget.TransformPoint(originalCameraLocalPosition);
                    playerCamera.transform.rotation = spectateFollowTarget.rotation * originalCameraLocalRotation;
                }
                else
                {
                    playerCamera.transform.position = spectateFollowTarget.position;
                    playerCamera.transform.rotation = spectateFollowTarget.rotation;
                }
            }

            if (IsKeyboardPlayer()) ConfineCursorToViewport();
        }

        private bool IsKeyboardPlayer() =>
            playerInput != null && playerInput.currentControlScheme == "Keyboard";

        private void ConfineCursorToViewport()
        {
            if (Mouse.current == null) return;

            float   minX     = originalViewportRect.x    * Screen.width;
            float   maxX     = originalViewportRect.xMax * Screen.width;
            float   minY     = originalViewportRect.y    * Screen.height;
            float   maxY     = originalViewportRect.yMax * Screen.height;
            Vector2 mousePos = Mouse.current.position.ReadValue();
            Vector2 clamped  = new Vector2(
                Mathf.Clamp(mousePos.x, minX, maxX),
                Mathf.Clamp(mousePos.y, minY, maxY));
            if (clamped != mousePos) Mouse.current.WarpCursorPosition(clamped);
        }

        // ═══════════════════════════════════════════════
        //  DEATH ENTRY POINT
        // ═══════════════════════════════════════════════

        public void OnPlayerDeath(string tag, float respawnDelay, Action onRespawn)
        {
            playerTag         = tag;
            respawnDuration   = respawnDelay;
            respawnTimer      = respawnDuration;
            onRespawnCallback = onRespawn;

            // In presentation focus mode — instant respawn, no death screen
            if (isSuppressed)
            {
                onRespawnCallback?.Invoke();
                onRespawnCallback = null;
                return;
            }

            isDead = true;

            ShowDeathScreen();
            DisablePlayerControls();

            if (IsKeyboardPlayer())
            {
                Cursor.visible   = true;
                Cursor.lockState = CursorLockMode.None;
            }

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
            GetComponent<CarController>()?.gameObject.GetComponent<CarController>()?.enabled.Equals(false);
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
        }

        private void StartSpectating()
        {
            if (alivePlayersCache.Count == 0)
            {
                spectateFollowTarget = null;
                UpdateSpectatingDisplay(noPlayersToSpectateText);
                UpdateNavigationButtons(false);
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
            if (target == null) { FindAlivePlayersToSpectate(); StartSpectating(); return; }
            SetSpectateTarget(target);
            UpdateSpectatingDisplay(target.tag);
        }

        private void SetSpectateTarget(GameObject target)
        {
            Camera targetCam = target.GetComponentInChildren<Camera>();
            if (targetCam != null)
            {
                spectateFollowTarget = targetCam.transform;
                isSpectatingBot      = false;
            }
            else
            {
                spectateFollowTarget = target.transform;
                isSpectatingBot      = true;
            }
        }

        private void CheckAlivePlayersCache()
        {
            aliveCheckTimer += Time.deltaTime;
            if (aliveCheckTimer < ALIVE_CHECK_INTERVAL) return;
            aliveCheckTimer = 0f;

            bool targetGone = spectateFollowTarget == null
                || alivePlayersCache.Count == 0
                || currentSpectateIndex >= alivePlayersCache.Count
                || alivePlayersCache[currentSpectateIndex] == null;

            if (targetGone) { FindAlivePlayersToSpectate(); StartSpectating(); }
        }

        private void SpectatePreviousPlayer()
        {
            if (alivePlayersCache.Count == 0) return;
            currentSpectateIndex--;
            if (currentSpectateIndex < 0) currentSpectateIndex = alivePlayersCache.Count - 1;
            SpectatePlayer(currentSpectateIndex);
        }

        private void SpectateNextPlayer()
        {
            if (alivePlayersCache.Count == 0) return;
            currentSpectateIndex++;
            if (currentSpectateIndex >= alivePlayersCache.Count) currentSpectateIndex = 0;
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
            if (previousPlayerButton != null) previousPlayerButton.interactable = interactable && moreThanOne;
            if (nextPlayerButton     != null) nextPlayerButton.interactable     = interactable && moreThanOne;
        }

        // ═══════════════════════════════════════════════
        //  TIMER
        // ═══════════════════════════════════════════════

        private void UpdateRespawnTimer()
        {
            respawnTimer -= Time.deltaTime;
            UpdateRespawnCountdownDisplay();
            if (respawnTimer <= 0f) OnRespawn();
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
            if (isSuppressed) return; // don't touch viewports in presentation mode
            if (playerCamera == null || originalCameraParent == null) return;
            playerCamera.transform.SetParent(originalCameraParent);
            playerCamera.transform.localPosition = originalCameraLocalPosition;
            playerCamera.transform.localRotation = originalCameraLocalRotation;
            playerCamera.rect                    = originalViewportRect;
            if (cinemachineBrain != null) cinemachineBrain.enabled = true;
        }

        // ═══════════════════════════════════════════════
        //  CLEANUP
        // ═══════════════════════════════════════════════

        private void OnDestroy()
        {
            if (previousPlayerButton != null) previousPlayerButton.onClick.RemoveAllListeners();
            if (nextPlayerButton     != null) nextPlayerButton.onClick.RemoveAllListeners();
        }
    }
}