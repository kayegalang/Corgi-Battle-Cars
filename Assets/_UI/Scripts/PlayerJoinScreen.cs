using System.Collections;
using System.Collections.Generic;
using _Audio.scripts;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Events;
using UnityEngine.UI;
using _Gameplay.Scripts;

namespace _UI.Scripts
{
    public class PlayerJoinScreen : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject joinPanel;
        [SerializeField] private GameObject characterSelectionPanel;
        [SerializeField] private List<PlayerJoinSlot> playerSlots = new List<PlayerJoinSlot>();

        [Header("Timing Settings")]
        [SerializeField] private float transitionDelay = 0.5f;

        [Header("Navigation")]
        [Tooltip("Panel to return to when pressing back (the PlayerCountSelector panel)")]
        [SerializeField] private GameObject previousPanel;

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
            public GameObject slotObject;

            [Header("Sprite States")]
            public Image  slotImage;
            public Sprite waitingSprite;
            public Sprite joinedSprite;
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

        private void Update()
        {
            // Controller B button — go back to player count selector
            if (joinPanel != null && joinPanel.activeSelf)
                if (Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame)
                    GoBack();
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
        //  BACK
        // ═══════════════════════════════════════════════

        public void GoBack()
        {
            StopAllCoroutines();
            CleanupExistingPlayers();
            DisablePlayerJoining();

            var guard = FindFirstObjectByType<PlayerOneUIGuard>();
            guard?.SetAllowAllDevices(false);

            joinedPlayerCount = 0;
            targetPlayerCount = 0;

            HideJoinPanel();

            if (previousPanel != null)
                previousPanel.SetActive(true);

            Debug.Log("[PlayerJoinScreen] Went back — join state fully reset.");
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

            return Gamepad.current.buttonSouth.isPressed   ||
                   Gamepad.current.buttonNorth.isPressed   ||
                   Gamepad.current.buttonEast.isPressed    ||
                   Gamepad.current.buttonWest.isPressed    ||
                   Gamepad.current.startButton.isPressed   ||
                   Gamepad.current.selectButton.isPressed  ||
                   Gamepad.current.leftShoulder.isPressed  ||
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
                    Debug.LogError($"[{nameof(PlayerJoinScreen)}] JoinPlayer returned null!");
                else
                {
                    joinedPlayer.camera = joinedPlayer.GetComponentInChildren<Camera>();
                    Debug.Log($"[PlayerJoinScreen] PlayerOne auto-joined! Camera assigned: {joinedPlayer.camera != null}");
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
        //  SLOTS — main's sprite swap system
        // ═══════════════════════════════════════════════

        private void InitializeSlots()
        {
            for (int i = 0; i < playerSlots.Count; i++)
            {
                if (i < targetPlayerCount)
                    ShowSlot(i);
                else
                    HideSlot(i);
            }
        }

        private void ShowSlot(int index)
        {
            if (!IsValidSlotIndex(index)) return;

            playerSlots[index].slotObject.SetActive(true);

            if (playerSlots[index].slotImage != null)
                playerSlots[index].slotImage.sprite = playerSlots[index].waitingSprite;
        }

        private void HideSlot(int index)
        {
            if (!IsValidSlotIndex(index)) return;
            playerSlots[index].slotObject.SetActive(false);
        }

        // ═══════════════════════════════════════════════
        //  PLAYER JOINED
        // ═══════════════════════════════════════════════

        private void OnPlayerJoined(PlayerInput playerInput)
        {
            joinedPlayerCount++;
            
            // Play join bark sound
            if (AudioManager.instance != null && FMODEvents.instance != null)
                AudioManager.instance.PlayOneShot(FMODEvents.instance.joinbark, transform.position);

            // Assign camera for split-screen (lives on ShakePivot child)
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

        private void UpdateSlotForJoinedPlayer()
        {
            int slotIndex = joinedPlayerCount - 1;
            if (!IsValidSlotIndex(slotIndex)) return;

            if (playerSlots[slotIndex].slotImage != null)
                playerSlots[slotIndex].slotImage.sprite = playerSlots[slotIndex].joinedSprite;
        }

        private bool AllPlayersHaveJoined() => joinedPlayerCount >= targetPlayerCount;

        private void OnAllPlayersJoined()
        {
            DisablePlayerJoining();
            onAllPlayersJoined?.Invoke(joinedPlayerCount);

            if (GameplayManager.instance != null)
                GameplayManager.instance.SetMultiplayerPlayerCount(joinedPlayerCount);

            StartCoroutine(TransitionToCharacterSelection());
        }

        private IEnumerator TransitionToCharacterSelection()
        {
            yield return new WaitForSeconds(transitionDelay);
            HideJoinScreen();

            if (characterSelectionPanel != null)
                characterSelectionPanel.SetActive(true);
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

        // ═══════════════════════════════════════════════
        //  CLEANUP / VALIDATION
        // ═══════════════════════════════════════════════

        private void CleanupExistingPlayers()
        {
            // Collect first then destroy — playerCount changes as we destroy
            var toDestroy = new List<PlayerInput>();
            for (int i = 0; i < PlayerInput.all.Count; i++)
            {
                PlayerInput player = PlayerInput.all[i];
                if (player != null)
                    toDestroy.Add(player);
            }

            foreach (PlayerInput player in toDestroy)
                if (player != null && player.gameObject != null)
                    Destroy(player.gameObject);

            Debug.Log($"[PlayerJoinScreen] Cleaned up {toDestroy.Count} player(s).");
        }

        private bool ValidatePlayerCount(int playerCount) =>
            playerCount > 0 && playerCount <= playerSlots.Count;

        private void ResetJoinState(int playerCount)
        {
            targetPlayerCount = playerCount;
            joinedPlayerCount = 0;
        }

        private void ShowJoinPanel()
        {
            if (joinPanel != null)
                joinPanel.SetActive(true);
        }

        private bool IsValidSlotIndex(int index) =>
            index >= 0 && index < playerSlots.Count;
    }
}