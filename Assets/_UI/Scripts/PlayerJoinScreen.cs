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
        [SerializeField] private string waitingText        = "Waiting...";
        [SerializeField] private string playerJoinedFormat = "Player {0} Joined!";
        
        [Header("Timing Settings")]
        [SerializeField] private float transitionDelay = 0.5f;
        
        [Header("Events")]
        public UnityEvent<int> onAllPlayersJoined;
        
        private PlayerInputManager playerInputManager;
        private int targetPlayerCount;
        private int joinedPlayerCount = 0;
        
        private const int FIRST_PLAYER_INDEX = 0;
        private const int ANY_DEVICE         = -1;
        
        [System.Serializable]
        public class PlayerJoinSlot
        {
            public GameObject  slotObject;
            public TMP_Text    statusText;
            public UnityEngine.UI.RawImage slotImage;
            public Color waitingColor = Color.gray;
            public Color joinedColor  = Color.green;
        }

        // ═══════════════════════════════════════════════
        //  LIFECYCLE
        // ═══════════════════════════════════════════════
        
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
                Debug.LogError($"[{nameof(PlayerJoinScreen)}] PlayerInputManager not found in scene!");
        }
        
        private void HideJoinPanel()
        {
            if (joinPanel != null)
                joinPanel.SetActive(false);
        }
        
        private void SubscribeToPlayerJoinedEvent()
        {
            if (playerInputManager != null)
                playerInputManager.onPlayerJoined += OnPlayerJoined;
        }
        
        private void UnsubscribeFromPlayerJoinedEvent()
        {
            if (playerInputManager != null)
                playerInputManager.onPlayerJoined -= OnPlayerJoined;
        }

        // ═══════════════════════════════════════════════
        //  SHOW / HIDE
        // ═══════════════════════════════════════════════
        
        public void ShowJoinScreen(int playerCount)
        {
            if (!ValidatePlayerCount(playerCount)) return;
            
            CleanupExistingPlayers();
            ResetJoinState(playerCount);
            ShowJoinPanel();
            InitializeSlots();

            var guard = FindFirstObjectByType<PlayerOneUIGuard>();
            guard?.SetAllowAllDevices(true);
            
            StartCoroutine(EnableJoiningAfterButtonRelease());
        }
        
        public void HideJoinScreen()
        {
            HideJoinPanel();
            DisablePlayerJoining();

            var guard = FindFirstObjectByType<PlayerOneUIGuard>();
            guard?.SetAllowAllDevices(false);
        }

        // ═══════════════════════════════════════════════
        //  JOIN FLOW
        // ═══════════════════════════════════════════════
        
        private IEnumerator EnableJoiningAfterButtonRelease()
        {
            if (Gamepad.current != null)
            {
                while (IsAnyGamepadButtonPressed())
                    yield return null;
            }
            
            if (Keyboard.current != null)
            {
                while (Keyboard.current.anyKey.isPressed)
                    yield return null;
            }
            
            yield return null;
            yield return new WaitForSeconds(0.2f);
            
            yield return StartCoroutine(AutoJoinFirstPlayer());
            EnablePlayerJoining();
        }
        
        private bool IsAnyGamepadButtonPressed()
        {
            if (Gamepad.current == null) return false;

            return Gamepad.current.buttonSouth.isPressed ||
                   Gamepad.current.buttonNorth.isPressed ||
                   Gamepad.current.buttonEast.isPressed  ||
                   Gamepad.current.buttonWest.isPressed  ||
                   Gamepad.current.startButton.isPressed ||
                   Gamepad.current.selectButton.isPressed ||
                   Gamepad.current.leftShoulder.isPressed ||
                   Gamepad.current.rightShoulder.isPressed;
        }
        
        private IEnumerator AutoJoinFirstPlayer()
        {
            if (joinedPlayerCount > 0) yield break;
            
            if (playerInputManager == null)
            {
                Debug.LogError($"[{nameof(PlayerJoinScreen)}] PlayerInputManager is null!");
                yield break;
            }
            
            string      controlScheme   = GetPlayerOneControlScheme();
            InputDevice playerOneDevice = _Player.Scripts.PlayerOneInputTracker.instance?.GetPlayerOneDevice();

            try
            {
                PlayerInput joinedPlayer;

                if (playerOneDevice != null)
                {
                    joinedPlayer = playerInputManager.JoinPlayer(
                        playerIndex:      FIRST_PLAYER_INDEX,
                        splitScreenIndex: ANY_DEVICE,
                        controlScheme:    controlScheme,
                        pairWithDevice:   playerOneDevice
                    );
                }
                else
                {
                    joinedPlayer = playerInputManager.JoinPlayer(
                        playerIndex:      FIRST_PLAYER_INDEX,
                        splitScreenIndex: ANY_DEVICE,
                        controlScheme:    controlScheme
                    );
                }

                if (joinedPlayer == null)
                {
                    Debug.LogError($"[{nameof(PlayerJoinScreen)}] JoinPlayer returned null!");
                }
                else
                {
                    // Camera lives on ShakePivot (child of PlayerCamera) not directly on root
                    // Unity can't find it automatically so we assign it manually
                    joinedPlayer.camera = joinedPlayer.GetComponentInChildren<Camera>();
                    Debug.Log($"[PlayerJoinScreen] PlayerOne camera assigned: {joinedPlayer.camera != null}");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[{nameof(PlayerJoinScreen)}] Failed to auto-join player: {e.Message}");
            }
        }
        
        private string GetPlayerOneControlScheme()
        {
            if (_Player.Scripts.PlayerOneInputTracker.instance != null &&
                _Player.Scripts.PlayerOneInputTracker.instance.IsPlayerOneUsingController())
                return "Controller";

            return "Keyboard";
        }

        // ═══════════════════════════════════════════════
        //  PLAYER INPUT MANAGER
        // ═══════════════════════════════════════════════
        
        private void EnablePlayerJoining()
        {
            if (playerInputManager != null)
                playerInputManager.EnableJoining();
        }
        
        private void DisablePlayerJoining()
        {
            if (playerInputManager != null)
                playerInputManager.DisableJoining();
        }

        // ═══════════════════════════════════════════════
        //  CLEANUP
        // ═══════════════════════════════════════════════
        
        private void CleanupExistingPlayers()
        {
            if (playerInputManager == null) return;
            
            List<PlayerInput> playersToRemove = new List<PlayerInput>();
            
            for (int i = 0; i < playerInputManager.playerCount; i++)
            {
                PlayerInput player = PlayerInput.GetPlayerByIndex(i);
                if (player != null)
                    playersToRemove.Add(player);
            }
            
            foreach (PlayerInput player in playersToRemove)
            {
                if (player != null && player.gameObject != null)
                    Destroy(player.gameObject);
            }
        }

        // ═══════════════════════════════════════════════
        //  VALIDATION
        // ═══════════════════════════════════════════════
        
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

        // ═══════════════════════════════════════════════
        //  SLOTS
        // ═══════════════════════════════════════════════
        
        private void ShowJoinPanel()
        {
            if (joinPanel != null)
                joinPanel.SetActive(true);
        }
        
        private void InitializeSlots()
        {
            for (int i = 0; i < playerSlots.Count; i++)
            {
                if (i < targetPlayerCount) ShowSlot(i);
                else                       HideSlot(i);
            }
        }
        
        private void ShowSlot(int index)
        {
            if (!IsValidSlotIndex(index)) return;

            playerSlots[index].slotObject.SetActive(true);
            playerSlots[index].statusText.text = waitingText;
            
            if (playerSlots[index].slotImage != null)
                playerSlots[index].slotImage.color = playerSlots[index].waitingColor;
        }
        
        private void HideSlot(int index)
        {
            if (!IsValidSlotIndex(index)) return;
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

        // ═══════════════════════════════════════════════
        //  ON PLAYER JOINED
        // ═══════════════════════════════════════════════
        
        private void OnPlayerJoined(PlayerInput playerInput)
        {
            joinedPlayerCount++;

            // Camera is on ShakePivot child — assign manually for split-screen
            playerInput.camera = playerInput.GetComponentInChildren<Camera>();

            // Track which device this player used
            if (playerInput.devices.Count > 0)
            {
                InputDevice device    = playerInput.devices[0];
                string      playerTag = GetPlayerTag(joinedPlayerCount);
                _Player.Scripts.PlayerDeviceTracker.instance?.RecordPlayerDevice(playerTag, device);
            }
            
            UpdateSlotForJoinedPlayer();
            
            if (AllPlayersHaveJoined())
                OnAllPlayersJoined();
        }
        
        private string GetPlayerTag(int playerNumber)
        {
            switch (playerNumber)
            {
                case 1:  return "PlayerOne";
                case 2:  return "PlayerTwo";
                case 3:  return "PlayerThree";
                case 4:  return "PlayerFour";
                default: return "PlayerOne";
            }
        }
        
        private bool AllPlayersHaveJoined() => joinedPlayerCount >= targetPlayerCount;
        
        private void UpdateSlotForJoinedPlayer()
        {
            if (joinedPlayerCount > playerSlots.Count)
            {
                Debug.LogWarning($"[{nameof(PlayerJoinScreen)}] More players joined than slots!");
                return;
            }
            
            int slotIndex = joinedPlayerCount - 1;
            UpdateSlotText(slotIndex);
            UpdateSlotColor(slotIndex);
        }
        
        private void UpdateSlotText(int slotIndex)
        {
            if (!IsValidSlotIndex(slotIndex)) return;
            playerSlots[slotIndex].statusText.text = string.Format(playerJoinedFormat, joinedPlayerCount);
        }
        
        private void UpdateSlotColor(int slotIndex)
        {
            if (!IsValidSlotIndex(slotIndex)) return;
            if (playerSlots[slotIndex].slotImage != null)
                playerSlots[slotIndex].slotImage.color = playerSlots[slotIndex].joinedColor;
        }

        // ═══════════════════════════════════════════════
        //  ALL PLAYERS JOINED
        // ═══════════════════════════════════════════════
        
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
                GameplayManager.instance.SetMultiplayerPlayerCount(joinedPlayerCount);
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