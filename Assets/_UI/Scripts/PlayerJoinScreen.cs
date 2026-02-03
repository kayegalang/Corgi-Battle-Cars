using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Events;
using TMPro;
using _Gameplay.Scripts;

namespace _UI.Scripts
{
    public class PlayerJoinScreen : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject joinPanel;
        [SerializeField] private GameObject characterSelectionPanel;
        [SerializeField] private List<PlayerJoinSlot> playerSlots = new List<PlayerJoinSlot>();
        
        [Header("UI Text")]
        [SerializeField] private string waitingText = "Waiting...";
        [SerializeField] private string playerJoinedFormat = "Player {0} Joined!";
        
        [Header("Timing Settings")]
        [SerializeField] private float autoJoinDelay = 0.1f;
        [SerializeField] private float transitionDelay = 0.5f;
        
        [Header("Events")]
        public UnityEvent<int> onAllPlayersJoined;
        
        private PlayerInputManager playerInputManager;
        private int targetPlayerCount;
        private int joinedPlayerCount = 0;
        
        private const int FIRST_PLAYER_INDEX = 0;
        private const int ANY_DEVICE = -1;
        
        [System.Serializable]
        public class PlayerJoinSlot
        {
            public GameObject slotObject;
            public TMP_Text statusText;
            public UnityEngine.UI.RawImage slotImage;
            public Color waitingColor = Color.gray;
            public Color joinedColor = Color.green;
        }
        
        private void Awake()
        {
            InitializePlayerInputManager();
            HideJoinPanel();
        }
        
        private void OnEnable()
        {
            SubscribeToPlayerJoinedEvent();
        }
        
        private void OnDisable()
        {
            UnsubscribeFromPlayerJoinedEvent();
        }
        
        private void InitializePlayerInputManager()
        {
            playerInputManager = FindFirstObjectByType<PlayerInputManager>();
            
            if (playerInputManager == null)
            {
                Debug.LogError($"[{nameof(PlayerJoinScreen)}] PlayerInputManager not found in scene!");
            }
        }
        
        private void HideJoinPanel()
        {
            if (joinPanel != null)
            {
                joinPanel.SetActive(false);
            }
        }
        
        private void SubscribeToPlayerJoinedEvent()
        {
            if (playerInputManager != null)
            {
                playerInputManager.onPlayerJoined += OnPlayerJoined;
            }
        }
        
        private void UnsubscribeFromPlayerJoinedEvent()
        {
            if (playerInputManager != null)
            {
                playerInputManager.onPlayerJoined -= OnPlayerJoined;
            }
        }
        
        public void ShowJoinScreen(int playerCount)
        {
            if (!ValidatePlayerCount(playerCount))
            {
                return;
            }
            
            // Clean up any existing players first
            CleanupExistingPlayers();
            
            ResetJoinState(playerCount);
            ShowJoinPanel();
            EnablePlayerJoining();
            InitializeSlots();
            StartCoroutine(AutoJoinFirstPlayer());
        }
        
        private void CleanupExistingPlayers()
        {
            if (playerInputManager == null)
            {
                return;
            }
            
            // Get all currently joined players
            List<PlayerInput> playersToRemove = new List<PlayerInput>();
            
            for (int i = 0; i < playerInputManager.playerCount; i++)
            {
                PlayerInput player = PlayerInput.GetPlayerByIndex(i);
                if (player != null)
                {
                    playersToRemove.Add(player);
                }
            }
            
            // Destroy all players
            foreach (PlayerInput player in playersToRemove)
            {
                if (player != null && player.gameObject != null)
                {
                    Destroy(player.gameObject);
                }
            }
        }
        
        private bool ValidatePlayerCount(int playerCount)
        {
            if (playerCount <= 0)
            {
                Debug.LogError($"[{nameof(PlayerJoinScreen)}] Invalid player count: {playerCount}");
                return false;
            }
            
            if (playerCount > playerSlots.Count)
            {
                Debug.LogError($"[{nameof(PlayerJoinScreen)}] Player count ({playerCount}) exceeds available slots ({playerSlots.Count})");
                return false;
            }
            
            return true;
        }
        
        private void ResetJoinState(int playerCount)
        {
            targetPlayerCount = playerCount;
            joinedPlayerCount = 0;
        }
        
        private void ShowJoinPanel()
        {
            if (joinPanel != null)
            {
                joinPanel.SetActive(true);
            }
        }
        
        private void EnablePlayerJoining()
        {
            if (playerInputManager != null)
            {
                playerInputManager.EnableJoining();
            }
        }
        
        private IEnumerator AutoJoinFirstPlayer()
        {
            yield return new WaitForSeconds(autoJoinDelay);
            
            Debug.Log($"[{nameof(PlayerJoinScreen)}] AutoJoinFirstPlayer - Current joined count: {joinedPlayerCount}");
            
            // Check if a player has already joined (from clicking the button)
            if (joinedPlayerCount > 0)
            {
                Debug.Log($"[{nameof(PlayerJoinScreen)}] Player already joined, skipping auto-join");
                yield break;
            }
            
            // Check if PlayerInputManager is ready
            if (playerInputManager == null)
            {
                Debug.LogError($"[{nameof(PlayerJoinScreen)}] PlayerInputManager is null!");
                yield break;
            }
            
            // Check if joining is enabled
            if (!playerInputManager.joiningEnabled)
            {
                Debug.LogWarning($"[{nameof(PlayerJoinScreen)}] Joining is not enabled, enabling it now");
                playerInputManager.EnableJoining();
            }
            
            Debug.Log($"[{nameof(PlayerJoinScreen)}] Auto-joining first player with device index {FIRST_PLAYER_INDEX}");
            
            try
            {
                PlayerInput joinedPlayer = playerInputManager.JoinPlayer(FIRST_PLAYER_INDEX, ANY_DEVICE, null);
                
                if (joinedPlayer != null)
                {
                    Debug.Log($"[{nameof(PlayerJoinScreen)}] Successfully auto-joined player: {joinedPlayer.gameObject.name}");
                }
                else
                {
                    Debug.LogWarning($"[{nameof(PlayerJoinScreen)}] JoinPlayer returned null!");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[{nameof(PlayerJoinScreen)}] Failed to auto-join player: {e.Message}");
            }
        }
        
        public void HideJoinScreen()
        {
            HideJoinPanel();
            DisablePlayerJoining();
        }
        
        private void DisablePlayerJoining()
        {
            if (playerInputManager != null)
            {
                playerInputManager.DisableJoining();
            }
        }
        
        private void InitializeSlots()
        {
            for (int i = 0; i < playerSlots.Count; i++)
            {
                if (i < targetPlayerCount)
                {
                    ShowSlot(i);
                }
                else
                {
                    HideSlot(i);
                }
            }
        }
        
        private void ShowSlot(int index)
        {
            if (!IsValidSlotIndex(index))
            {
                return;
            }
            
            playerSlots[index].slotObject.SetActive(true);
            playerSlots[index].statusText.text = waitingText;
            
            if (playerSlots[index].slotImage != null)
            {
                playerSlots[index].slotImage.color = playerSlots[index].waitingColor;
            }
        }
        
        private void HideSlot(int index)
        {
            if (!IsValidSlotIndex(index))
            {
                return;
            }
            
            playerSlots[index].slotObject.SetActive(false);
        }
        
        private bool IsValidSlotIndex(int index)
        {
            if (index < 0 || index >= playerSlots.Count)
            {
                Debug.LogError($"[{nameof(PlayerJoinScreen)}] Invalid slot index: {index}");
                return false;
            }
            
            return true;
        }
        
        private void OnPlayerJoined(PlayerInput playerInput)
        {
            Debug.Log($"[{nameof(PlayerJoinScreen)}] OnPlayerJoined called - Player: {playerInput.gameObject.name}, Device: {playerInput.devices[0].displayName}");
            
            joinedPlayerCount++;
            
            Debug.Log($"[{nameof(PlayerJoinScreen)}] Total joined players: {joinedPlayerCount}/{targetPlayerCount}");
            
            UpdateSlotForJoinedPlayer();
            
            if (AllPlayersHaveJoined())
            {
                OnAllPlayersJoined();
            }
        }
        
        private bool AllPlayersHaveJoined()
        {
            return joinedPlayerCount >= targetPlayerCount;
        }
        
        private void UpdateSlotForJoinedPlayer()
        {
            if (joinedPlayerCount > playerSlots.Count)
            {
                Debug.LogWarning($"[{nameof(PlayerJoinScreen)}] More players joined than available slots!");
                return;
            }
            
            int slotIndex = joinedPlayerCount - 1;
            
            UpdateSlotText(slotIndex);
            UpdateSlotColor(slotIndex);
        }
        
        private void UpdateSlotText(int slotIndex)
        {
            if (!IsValidSlotIndex(slotIndex))
            {
                return;
            }
            
            playerSlots[slotIndex].statusText.text = string.Format(playerJoinedFormat, joinedPlayerCount);
        }
        
        private void UpdateSlotColor(int slotIndex)
        {
            if (!IsValidSlotIndex(slotIndex))
            {
                return;
            }
            
            if (playerSlots[slotIndex].slotImage != null)
            {
                playerSlots[slotIndex].slotImage.color = playerSlots[slotIndex].joinedColor;
            }
        }
        
        private void OnAllPlayersJoined()
        {
            DisablePlayerJoining();
            InvokeAllPlayersJoinedEvent();
            NotifyGameplayManager();
            StartCoroutine(TransitionToCharacterSelection());
        }
        
        private void InvokeAllPlayersJoinedEvent()
        {
            onAllPlayersJoined?.Invoke(joinedPlayerCount);
        }
        
        private void NotifyGameplayManager()
        {
            if (GameplayManager.instance != null)
            {
                GameplayManager.instance.SetMultiplayerPlayerCount(joinedPlayerCount);
            }
        }
        
        private IEnumerator TransitionToCharacterSelection()
        {
            yield return new WaitForSeconds(transitionDelay);
            HideJoinScreen();
            ShowCharacterSelection();
        }
        
        private void ShowCharacterSelection()
        {
            if (characterSelectionPanel == null)
            {
                Debug.LogWarning($"[{nameof(PlayerJoinScreen)}] CharacterSelectionPanel reference not set!");
                return;
            }
            
            characterSelectionPanel.SetActive(true);
        }
        
        public void GoBack()
        {
            CleanupExistingPlayers();
        }
    }
}