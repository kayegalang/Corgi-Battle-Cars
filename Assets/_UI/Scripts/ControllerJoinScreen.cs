using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using _Gameplay.Scripts;

namespace _UI.Scripts
{
    public class ControllerJoinScreen : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject joinPanel; // The panel to show/hide
        [SerializeField] private GameObject characterSelectionPanel; // Next panel to show
        [SerializeField] private List<PlayerJoinSlot> playerSlots = new List<PlayerJoinSlot>(); // UI slots for each player
        
        private PlayerInputManager playerInputManager;
        private int targetPlayerCount;
        private int joinedPlayerCount = 0;
        
        [System.Serializable]
        public class PlayerJoinSlot
        {
            public GameObject slotObject;
            public TMP_Text statusText;
            public UnityEngine.UI.RawImage slotImage; // RawImage that changes color
            public Color waitingColor = Color.gray; // Color when waiting
            public Color joinedColor = Color.green; // Color when joined
        }
        
        private void Awake()
        {
            // Find or create PlayerInputManager
            playerInputManager = FindFirstObjectByType<PlayerInputManager>();
            
            if (playerInputManager == null)
            {
                Debug.LogError("PlayerInputManager not found in scene!");
                return;
            }
            
            // Hide the panel initially
            if (joinPanel != null)
                joinPanel.SetActive(false);
        }
        
        // Call this method to show the join screen with the selected player count
        public void ShowJoinScreen(int playerCount)
        {
            targetPlayerCount = playerCount;
            joinedPlayerCount = 0;
            
            // Make sure PlayerInputManager persists across scene loads
            if (playerInputManager != null)
            {
                DontDestroyOnLoad(playerInputManager.gameObject);
                Debug.Log("PlayerInputManager set to DontDestroyOnLoad");
            }
            
            // Show the panel
            if (joinPanel != null)
                joinPanel.SetActive(true);
            
            // Enable joining
            playerInputManager.EnableJoining();
            
            Debug.Log($"Waiting for {targetPlayerCount} players to join");
            
            // Initialize UI slots
            InitializeSlots();
            
            // Auto-join Player 1 after a short delay (let everything initialize)
            Invoke(nameof(AutoJoinPlayer1), 0.1f);
        }
        
        // Hide the join screen
        public void HideJoinScreen()
        {
            if (joinPanel != null)
                joinPanel.SetActive(false);
            
            // Disable joining
            if (playerInputManager != null)
                playerInputManager.DisableJoining();
        }
        
        private void InitializeSlots()
        {
            for (int i = 0; i < playerSlots.Count; i++)
            {
                if (i < targetPlayerCount)
                {
                    // Show this slot
                    playerSlots[i].slotObject.SetActive(true);
                    
                    // All slots start as "Waiting..." in gray
                    playerSlots[i].statusText.text = "Waiting...";
                    
                    // Set to waiting color (gray)
                    if (playerSlots[i].slotImage != null)
                        playerSlots[i].slotImage.color = playerSlots[i].waitingColor;
                }
                else
                {
                    // Hide unused slots
                    playerSlots[i].slotObject.SetActive(false);
                }
            }
        }
        
        private void AutoJoinPlayer1()
        {
            // Force Player 1 to join automatically
            if (playerInputManager != null)
            {
                // This will trigger OnPlayerJoined callback
                playerInputManager.JoinPlayer(0, -1, null);
                Debug.Log("Player 1 auto-joined!");
            }
        }
        
        private void OnEnable()
        {
            if (playerInputManager != null)
            {
                playerInputManager.onPlayerJoined += OnPlayerJoined;
            }
        }
        
        private void OnDisable()
        {
            if (playerInputManager != null)
            {
                playerInputManager.onPlayerJoined -= OnPlayerJoined;
            }
        }
        
        private void OnPlayerJoined(PlayerInput playerInput)
        {
            joinedPlayerCount++;
            
            Debug.Log($"Player {joinedPlayerCount} joined! ({joinedPlayerCount}/{targetPlayerCount})");
            
            // Update UI for this player
            if (joinedPlayerCount <= playerSlots.Count)
            {
                int slotIndex = joinedPlayerCount - 1;
                
                // Set text to "Player X Joined!"
                playerSlots[slotIndex].statusText.text = $"Player {joinedPlayerCount} Joined!";
                
                // Change image color to green (joined)
                if (playerSlots[slotIndex].slotImage != null)
                    playerSlots[slotIndex].slotImage.color = playerSlots[slotIndex].joinedColor;
            }
            
            // Check if all players have joined
            if (joinedPlayerCount >= targetPlayerCount)
            {
                Debug.Log("All players joined! Moving to Character Selection");
                playerInputManager.DisableJoining();
                
                // Set the multiplayer player count in GameplayManager
                if (GameplayManager.instance != null)
                {
                    GameplayManager.instance.SetMultiplayerPlayerCount(joinedPlayerCount);
                }
                
                // Immediately transition to character selection
                HideJoinScreen();
                ShowCharacterSelection();
            }
        }
        
        private void ShowCharacterSelection()
        {
            if (characterSelectionPanel != null)
            {
                characterSelectionPanel.SetActive(true);
            }
            else
            {
                Debug.LogWarning("CharacterSelectionPanel reference not set in ControllerJoinScreen!");
            }
        }
        
        // Call this to cancel and go back to player count selection
        public void GoBack()
        {
            HideJoinScreen();
            // You can call your player count selection panel's show method here
        }
    }
}