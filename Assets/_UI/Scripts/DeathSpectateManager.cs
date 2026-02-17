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
        [SerializeField] private GameObject deathScreenPanel;
        [SerializeField] private TextMeshProUGUI respawnCountdownText;
        [SerializeField] private TextMeshProUGUI spectatingText;
        [SerializeField] private Button previousPlayerButton;
        [SerializeField] private Button nextPlayerButton;

        [Header("UI Text")]
        [SerializeField] private string respawnCountdownFormat = "Respawning in {0}";
        [SerializeField] private string spectatingFormat = "Spectating: {0}";
        [SerializeField] private string noPlayersToSpectateText = "No players to spectate";

        private Camera playerCamera;
        private CinemachineBrain cinemachineBrain;
        private PlayerInput playerInput;
        private string playerTag;
        private bool isDead = false;

        private float respawnTimer = 0f;
        private float respawnDuration = 3f;
        private Action onRespawnCallback;

        private List<GameObject> alivePlayersCache = new List<GameObject>();
        private int currentSpectateIndex = 0;

        // Bookmarks so we can restore the camera when the player respawns
        private Transform originalCameraParent;
        private Vector3 originalCameraLocalPosition;
        private Quaternion originalCameraLocalRotation;

        private Rect originalViewportRect;

        private const float ALIVE_CHECK_INTERVAL = 0.5f;
        private float aliveCheckTimer = 0f;

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
            playerCamera = GetComponentInChildren<Camera>();
            cinemachineBrain = GetComponentInChildren<CinemachineBrain>();
            playerInput = GetComponent<PlayerInput>();

            if (playerCamera == null)
            {
                Debug.LogError($"[{nameof(DeathSpectateManager)}] No camera found on {gameObject.name}!");
                return;
            }

            originalViewportRect = playerCamera.rect;

            SaveOriginalCameraDetails();
        }

        private void SaveOriginalCameraDetails()
        {
            originalCameraParent = playerCamera.transform.parent;
            originalCameraLocalPosition = playerCamera.transform.localPosition;
            originalCameraLocalRotation = playerCamera.transform.localRotation;
        }

        private void ConstrainPanelToViewport()
        {
            if (deathScreenPanel == null || playerCamera == null) return;

            RectTransform panelRect = deathScreenPanel.GetComponent<RectTransform>();
            if (panelRect == null) return;

            panelRect.anchorMin = new Vector2(originalViewportRect.x, originalViewportRect.y);
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

        private void Update()
        {
            if (!isDead) return;

            UpdateRespawnTimer();
            CheckAlivePlayersCache();
        }

        private void LateUpdate()
        {
            if (!isDead) return;
            
            playerCamera.rect = originalViewportRect;
            
            if (IsKeyboardPlayer())
            {
                ConfineCursorToViewport();
            }
        }

        private bool IsKeyboardPlayer()
        {
            return playerInput != null && playerInput.currentControlScheme == "Keyboard";
        }

        private void ConfineCursorToViewport()
        {
            if (Mouse.current == null) return;

            // Convert normalised viewport rect to actual screen pixels
            float minX = originalViewportRect.x * Screen.width;
            float maxX = originalViewportRect.xMax * Screen.width;
            float minY = originalViewportRect.y * Screen.height;
            float maxY = originalViewportRect.yMax * Screen.height;

            Vector2 mousePos = Mouse.current.position.ReadValue();
            Vector2 clamped = new Vector2(
                Mathf.Clamp(mousePos.x, minX, maxX),
                Mathf.Clamp(mousePos.y, minY, maxY)
            );

            if (clamped != mousePos)
                Mouse.current.WarpCursorPosition(clamped);
        }

        public void OnPlayerDeath(string tag, float respawnDelay, Action onRespawn)
        {
            playerTag = tag;
            respawnDuration = respawnDelay;
            respawnTimer = respawnDuration;
            onRespawnCallback = onRespawn;
            isDead = true;

            ShowDeathScreen();
            DisablePlayerControls();
            
            if (IsKeyboardPlayer())
            {
                Cursor.visible = true;
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
            CarController carController = GetComponent<CarController>();
            if (carController != null)
                carController.enabled = false;

            CarShooter carShooter = GetComponent<CarShooter>();
            if (carShooter != null)
                carShooter.enabled = false;
        }

        private void FindAlivePlayersToSpectate()
        {
            alivePlayersCache.Clear();

            CarHealth[] allCars = FindObjectsByType<CarHealth>(FindObjectsSortMode.None);

            foreach (CarHealth car in allCars)
            {
                if (car.gameObject.CompareTag(playerTag)) continue;
                if (car.IsDead()) continue;

                alivePlayersCache.Add(car.gameObject);
            }

            Debug.Log($"[{nameof(DeathSpectateManager)}] {playerTag} found {alivePlayersCache.Count} targets to spectate");
        }

        private void StartSpectating()
        {
            if (alivePlayersCache.Count == 0)
            {
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

            if (target == null)
            {
                FindAlivePlayersToSpectate();
                StartSpectating();
                return;
            }

            AttachCameraToPlayer(target);
            UpdateSpectatingDisplay(target.tag);
        }

        private void AttachCameraToPlayer(GameObject target)
        {
            if (playerCamera == null) return;

            Camera targetCamera = target.GetComponentInChildren<Camera>();

            if (targetCamera != null)
            {
                playerCamera.transform.SetParent(targetCamera.transform.parent);
                playerCamera.transform.localPosition = targetCamera.transform.localPosition;
                playerCamera.transform.localRotation = targetCamera.transform.localRotation;
            }
            else
            {
                playerCamera.transform.SetParent(target.transform);
                playerCamera.transform.localPosition = originalCameraLocalPosition;
                playerCamera.transform.localRotation = originalCameraLocalRotation;
            }
        }

        private void CheckAlivePlayersCache()
        {
            aliveCheckTimer += Time.deltaTime;
            if (aliveCheckTimer < ALIVE_CHECK_INTERVAL) return;

            aliveCheckTimer = 0f;

            bool currentTargetGone = alivePlayersCache.Count == 0
                || currentSpectateIndex >= alivePlayersCache.Count
                || alivePlayersCache[currentSpectateIndex] == null;

            if (currentTargetGone)
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

        private void OnRespawn()
        {
            isDead = false;

            HideDeathScreen();
            RestoreCamera();

            onRespawnCallback?.Invoke();
            onRespawnCallback = null;
        }

        private void RestoreCamera()
        {
            if (playerCamera == null || originalCameraParent == null) return;

            // Undo the SetParent from spectating
            playerCamera.transform.SetParent(originalCameraParent);
            playerCamera.transform.localPosition = originalCameraLocalPosition;
            playerCamera.transform.localRotation = originalCameraLocalRotation;
            playerCamera.rect = originalViewportRect;

            // Hand control back to Cinemachine
            if (cinemachineBrain != null)
                cinemachineBrain.enabled = true;
        }

        private void OnDestroy()
        {
            if (previousPlayerButton != null)
                previousPlayerButton.onClick.RemoveAllListeners();

            if (nextPlayerButton != null)
                nextPlayerButton.onClick.RemoveAllListeners();
        }
    }
}