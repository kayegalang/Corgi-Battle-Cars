using UnityEngine;

namespace _UI.Scripts
{
    public class StartScreenController : MonoBehaviour
    {
        [SerializeField] private GameObject[] scenePanels;
        [SerializeField] private GameObject startScreen;
        [SerializeField] private GameObject mainMenu;

        private static bool hasSeenStartScreen = false;
        
        void Start()
        {
            bool showStartScreen = !hasSeenStartScreen;
            
            startScreen.SetActive(showStartScreen);
            mainMenu.SetActive(!showStartScreen);
            
            if (showStartScreen)
            {
                hasSeenStartScreen = true;
            }
            
            DeactivatePanels();
        }

        private void DeactivatePanels()
        {
            foreach (GameObject panel in scenePanels)
            {
                panel.SetActive(false);
            }
        }
    }
}