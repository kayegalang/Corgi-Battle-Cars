using System;
using System.Collections.Generic;
using System.Linq;
using _Cars.Scripts;
using TMPro;
using UnityEngine;
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
        private string playerTag;
        private bool isDead = false;
        
        private float respawnTimer = 0f;
        private float respawnDuration = 3f;
        private Action onRespawnCallback;
        
        private List<GameObject> alivePlayersCache = new List<GameObject>();
        private int currentSpectateIndex = 0;
        
        private Vector3 originalCameraLocalPosition;
        private Quaternion originalCameraLocalRotation;
        private Transform originalCameraParent;
        
        private const float CAMERA_UPDATE_INTERVAL = 0.5f;
        private float cameraUpdateTimer = 0f;
        
        private void Awake()
        {
            HideDeathScreen();
            SetupButtons();
        }
        
        private void Start()
        {
            InitializeCamera();
        }
        
        private void Update()
        {
            if (!isDead)
            {
                return;
            }
            
            UpdateRespawnTimer();
            UpdateSpectateCamera();
        }
        
        private void InitializeCamera()
        {
            playerCamera = GetComponentInChildren<Camera>();
            
            if (playerCamera != null)
            {
                originalCameraParent = playerCamera.transform.parent;
                originalCameraLocalPosition = playerCamera.transform.localPosition;
                originalCameraLocalRotation = playerCamera.transform.localRotation;
            }
            else
            {
                Debug.LogError($"[{nameof(DeathSpectateManager)}] No camera found on {gameObject.name}!");
            }
        }
        
        private void SetupButtons()
        {
            if (previousPlayerButton != null)
            {
                previousPlayerButton.onClick.AddListener(SpectatePreviousPlayer);
            }
            
            if (nextPlayerButton != null)
            {
                nextPlayerButton.onClick.AddListener(SpectateNextPlayer);
            }
        }
        
        private void HideDeathScreen()
        {
            if (deathScreenPanel != null)
            {
                deathScreenPanel.SetActive(false);
            }
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
            FindAlivePlayersToSpectate();
            StartSpectating();
        }
        
        private void ShowDeathScreen()
        {
            if (deathScreenPanel != null)
            {
                deathScreenPanel.SetActive(true);
            }
            
            UpdateRespawnCountdownDisplay();
        }
        
        private void DisablePlayerControls()
        {
            // Disable car controller
            CarController carController = GetComponent<CarController>();
            if (carController != null)
            {
                carController.enabled = false;
            }
            
            // Disable car shooter
            CarShooter carShooter = GetComponent<CarShooter>();
            if (carShooter != null)
            {
                carShooter.enabled = false;
            }
        }
        
        private void FindAlivePlayersToSpectate()
        {
            alivePlayersCache.Clear();
            
            // Find all CarHealth components
            CarHealth[] allPlayers = FindObjectsByType<CarHealth>(FindObjectsSortMode.None);
            
            foreach (CarHealth player in allPlayers)
            {
                // Don't include ourselves or dead players
                if (player.gameObject != gameObject && !player.IsDead())
                {
                    alivePlayersCache.Add(player.gameObject);
                }
            }
            
            Debug.Log($"[{nameof(DeathSpectateManager)}] Found {alivePlayersCache.Count} alive players to spectate");
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
            if (index < 0 || index >= alivePlayersCache.Count)
            {
                return;
            }
            
            GameObject targetPlayer = alivePlayersCache[index];
            
            if (targetPlayer == null)
            {
                // Player was destroyed, refresh list
                FindAlivePlayersToSpectate();
                return;
            }
            
            AttachCameraToPlayer(targetPlayer);
            UpdateSpectatingDisplay(targetPlayer.tag);
        }
        
        private void AttachCameraToPlayer(GameObject targetPlayer)
        {
            if (playerCamera == null)
            {
                return;
            }
            
            // Find the target's camera position (where their camera would be)
            Camera targetCamera = targetPlayer.GetComponentInChildren<Camera>();
            
            if (targetCamera != null)
            {
                // Attach our camera to the same parent as their camera
                playerCamera.transform.SetParent(targetCamera.transform.parent);
                playerCamera.transform.localPosition = targetCamera.transform.localPosition;
                playerCamera.transform.localRotation = targetCamera.transform.localRotation;
            }
            else
            {
                // Fallback: just follow the player
                playerCamera.transform.SetParent(targetPlayer.transform);
                playerCamera.transform.localPosition = originalCameraLocalPosition;
                playerCamera.transform.localRotation = originalCameraLocalRotation;
            }
        }
        
        private void UpdateSpectatingDisplay(string displayText)
        {
            if (spectatingText != null)
            {
                spectatingText.text = string.Format(spectatingFormat, displayText);
            }
        }
        
        private void UpdateNavigationButtons(bool enabled)
        {
            if (previousPlayerButton != null)
            {
                previousPlayerButton.interactable = enabled && alivePlayersCache.Count > 1;
            }
            
            if (nextPlayerButton != null)
            {
                nextPlayerButton.interactable = enabled && alivePlayersCache.Count > 1;
            }
        }
        
        private void SpectatePreviousPlayer()
        {
            if (alivePlayersCache.Count == 0)
            {
                return;
            }
            
            currentSpectateIndex--;
            if (currentSpectateIndex < 0)
            {
                currentSpectateIndex = alivePlayersCache.Count - 1;
            }
            
            SpectatePlayer(currentSpectateIndex);
        }
        
        private void SpectateNextPlayer()
        {
            if (alivePlayersCache.Count == 0)
            {
                return;
            }
            
            currentSpectateIndex++;
            if (currentSpectateIndex >= alivePlayersCache.Count)
            {
                currentSpectateIndex = 0;
            }
            
            SpectatePlayer(currentSpectateIndex);
        }
        
        private void UpdateSpectateCamera()
        {
            // Periodically refresh the alive players list
            cameraUpdateTimer += Time.deltaTime;
            
            if (cameraUpdateTimer >= CAMERA_UPDATE_INTERVAL)
            {
                cameraUpdateTimer = 0f;
                
                // Check if current spectate target is still alive
                if (alivePlayersCache.Count > 0 && currentSpectateIndex < alivePlayersCache.Count)
                {
                    GameObject currentTarget = alivePlayersCache[currentSpectateIndex];
                    
                    if (currentTarget == null)
                    {
                        // Target was destroyed, refresh and find new target
                        FindAlivePlayersToSpectate();
                        StartSpectating();
                    }
                }
            }
        }
        
        private void UpdateRespawnTimer()
        {
            respawnTimer -= Time.deltaTime;
            
            UpdateRespawnCountdownDisplay();
            
            if (respawnTimer <= 0f)
            {
                OnRespawn();
            }
        }
        
        private void UpdateRespawnCountdownDisplay()
        {
            if (respawnCountdownText != null)
            {
                int seconds = Mathf.CeilToInt(respawnTimer);
                respawnCountdownText.text = string.Format(respawnCountdownFormat, seconds);
            }
        }
        
        private void OnRespawn()
        {
            isDead = false;
            
            HideDeathScreen();
            RestoreCamera();
            
            // Fire the respawn callback - happens at the exact same time as UI hiding
            onRespawnCallback?.Invoke();
            onRespawnCallback = null;
        }
        
        private void RestoreCamera()
        {
            if (playerCamera == null || originalCameraParent == null)
            {
                return;
            }
            
            playerCamera.transform.SetParent(originalCameraParent);
            playerCamera.transform.localPosition = originalCameraLocalPosition;
            playerCamera.transform.localRotation = originalCameraLocalRotation;
        }
        
        private void OnDestroy()
        {
            CleanupButtons();
        }
        
        private void CleanupButtons()
        {
            if (previousPlayerButton != null)
            {
                previousPlayerButton.onClick.RemoveAllListeners();
            }
            
            if (nextPlayerButton != null)
            {
                nextPlayerButton.onClick.RemoveAllListeners();
            }
        }
    }
}