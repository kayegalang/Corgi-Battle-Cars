using UnityEngine;
using UnityEngine.UI;

namespace _UI.Scripts
{
    public class PlayerCountSelector : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject selectionPanel; // This panel
        [SerializeField] private ControllerJoinScreen joinScreen; // Reference to the join screen
        [SerializeField] private Button twoPlayersButton;
        [SerializeField] private Button threePlayersButton;
        [SerializeField] private Button fourPlayersButton;
        
        private void Start()
        {
            // Set up button listeners
            if (twoPlayersButton != null)
                twoPlayersButton.onClick.AddListener(() => SelectPlayerCount(2));
            
            if (threePlayersButton != null)
                threePlayersButton.onClick.AddListener(() => SelectPlayerCount(3));
            
            if (fourPlayersButton != null)
                fourPlayersButton.onClick.AddListener(() => SelectPlayerCount(4));
            
            // Make sure selection panel is visible
            if (selectionPanel != null)
                selectionPanel.SetActive(true);
        }
        
        private void SelectPlayerCount(int playerCount)
        {
            // Hide this panel
            if (selectionPanel != null)
                selectionPanel.SetActive(false);
            
            // Show the join screen with the selected player count
            if (joinScreen != null)
            {
                joinScreen.ShowJoinScreen(playerCount);
            }
            else
            {
                Debug.LogError("ControllerJoinScreen reference not set!");
            }
        }
        
        // Call this to show the selection panel again (e.g., from back button)
        public void ShowSelectionPanel()
        {
            if (selectionPanel != null)
                selectionPanel.SetActive(true);
        }
    }
}