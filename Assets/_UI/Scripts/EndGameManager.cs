using _Gameplay.Scripts;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _UI.Scripts
{
    public class EndGameManager : MonoBehaviour
    {
        [SerializeField] private GameObject endScreen;

        public void OnGameEnd()
        {
            endScreen.SetActive(true);
        }

        public void OnMainMenuButtonClicked()
        {
            SceneManager.LoadScene("MainMenu");
        }

        public void OnPlayAgainButtonClicked()
        {
            GameplayManager.instance.StartGame();
        }
    }
}
