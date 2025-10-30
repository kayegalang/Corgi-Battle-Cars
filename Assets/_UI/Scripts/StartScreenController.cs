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
            if (!hasSeenStartScreen)
            {
                startScreen.SetActive(true);
                mainMenu.SetActive(false);
                hasSeenStartScreen = true;
            }
            else
            {
                startScreen.SetActive(false);
                mainMenu.SetActive(true);
            }
            
            foreach (GameObject panel in scenePanels)
            {
                panel.SetActive(false);
            }
        }
    }
}

