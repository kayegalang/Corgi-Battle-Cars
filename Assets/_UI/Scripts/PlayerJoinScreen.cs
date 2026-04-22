using System.Collections;
using System.Collections.Generic;
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

            [Header("Sprite States")]
            public Image slotImage;
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

            StartCoroutine(EnableJoiningAfterButtonRelease());
        }

        public void HideJoinScreen()
        {
            HideJoinPanel();
            DisablePlayerJoining();
        }

        private IEnumerator EnableJoiningAfterButtonRelease()
        {
            yield return null;
            yield return new WaitForSeconds(0.2f);

            EnablePlayerJoining();
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
        //  SLOTS
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

            UpdateSlotForJoinedPlayer();

            if (AllPlayersHaveJoined())
                OnAllPlayersJoined();
        }

        private void UpdateSlotForJoinedPlayer()
        {
            int slotIndex = joinedPlayerCount - 1;

            if (!IsValidSlotIndex(slotIndex)) return;

            if (playerSlots[slotIndex].slotImage != null)
                playerSlots[slotIndex].slotImage.sprite =
                    playerSlots[slotIndex].joinedSprite;
        }

        private bool AllPlayersHaveJoined()
        {
            return joinedPlayerCount >= targetPlayerCount;
        }

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

        // ═══════════════════════════════════════════════
        //  CLEANUP / VALIDATION
        // ═══════════════════════════════════════════════

        private void CleanupExistingPlayers()
        {
            for (int i = 0; i < PlayerInputManager.instance.playerCount; i++)
            {
                PlayerInput player = PlayerInput.GetPlayerByIndex(i);
                if (player != null)
                    Destroy(player.gameObject);
            }
        }

        private bool ValidatePlayerCount(int playerCount)
        {
            return playerCount > 0 && playerCount <= playerSlots.Count;
        }

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

        private bool IsValidSlotIndex(int index)
        {
            return index >= 0 && index < playerSlots.Count;
        }
    }
}