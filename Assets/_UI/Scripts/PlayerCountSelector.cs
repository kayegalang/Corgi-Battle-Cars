using UnityEngine;
using UnityEngine.UI;

namespace _UI.Scripts
{
    public class PlayerCountSelector : MonoBehaviour
    {
        [Header("UI References")]
        [Tooltip("Panel containing the player count selection buttons")]
        [SerializeField] private GameObject selectionPanel;
        
        [Tooltip("The join screen that will be shown after player count is selected")]
        [SerializeField] private PlayerJoinScreen joinScreen;
        
        [Tooltip("Button to select 2 players")]
        [SerializeField] private Button twoPlayersButton;
        
        [Tooltip("Button to select 3 players")]
        [SerializeField] private Button threePlayersButton;
        
        [Tooltip("Button to select 4 players")]
        [SerializeField] private Button fourPlayersButton;
        
        private const int MIN_PLAYER_COUNT = 1;
        private const int MAX_PLAYER_COUNT = 4;
        
        private void Start()
        {
            ValidateReferences();
            InitializeButtons();
            ShowSelectionPanel();
        }
        
        private void OnDestroy()
        {
            CleanupButtonListeners();
        }
        
        private void ValidateReferences()
        {
            if (selectionPanel == null)
            {
                Debug.LogError($"[{nameof(PlayerCountSelector)}] Selection panel is not assigned!", this);
            }
            
            if (joinScreen == null)
            {
                Debug.LogError($"[{nameof(PlayerCountSelector)}] Join screen is not assigned!", this);
            }
        }
        
        private void InitializeButtons()
        {
            SetupButton(twoPlayersButton, 2);
            SetupButton(threePlayersButton, 3);
            SetupButton(fourPlayersButton, 4);
        }
        
        private void ShowSelectionPanel()
        {
            if (selectionPanel != null)
            {
                selectionPanel.SetActive(true);
            }
        }
        
        private void SetupButton(Button button, int playerCount)
        {
            if (button == null)
            {
                Debug.LogWarning($"[{nameof(PlayerCountSelector)}] Button for {playerCount} players is not assigned!");
                return;
            }
            
            if (!IsValidPlayerCount(playerCount))
            {
                Debug.LogError($"[{nameof(PlayerCountSelector)}] Invalid player count: {playerCount}. Must be between {MIN_PLAYER_COUNT} and {MAX_PLAYER_COUNT}");
                return;
            }
            
            button.onClick.AddListener(() => SelectPlayerCount(playerCount));
        }
        
        public void SelectPlayerCount(int playerCount)
        {
            if (!IsValidPlayerCount(playerCount))
            {
                Debug.LogError($"[{nameof(PlayerCountSelector)}] Invalid player count selected: {playerCount}");
                return;
            }
            
            HideSelectionPanel();
            ShowJoinScreen(playerCount);
        }
        
        private void HideSelectionPanel()
        {
            if (selectionPanel != null)
            {
                selectionPanel.SetActive(false);
            }
        }
        
        private void ShowJoinScreen(int playerCount)
        {
            if (joinScreen == null)
            {
                Debug.LogError($"[{nameof(PlayerCountSelector)}] Cannot show join screen - reference not set!");
                return;
            }
            
            joinScreen.ShowJoinScreen(playerCount);
        }
        
        private bool IsValidPlayerCount(int playerCount)
        {
            return playerCount >= MIN_PLAYER_COUNT && playerCount <= MAX_PLAYER_COUNT;
        }
        
        private void CleanupButtonListeners()
        {
            RemoveButtonListener(twoPlayersButton);
            RemoveButtonListener(threePlayersButton);
            RemoveButtonListener(fourPlayersButton);
        }
        
        private void RemoveButtonListener(Button button)
        {
            if (button != null)
            {
                button.onClick.RemoveAllListeners();
            }
        }
    }
}