using UnityEngine;

public class GameplayManager : MonoBehaviour
{
    public static GameplayManager instance;
    public GameObject PauseScreen;
    public GameObject GameplayPanel;
    
    private bool isPaused = false;
    void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    

    public void PauseGame()
    {
        GameplayPanel.SetActive(false);
        PauseScreen.SetActive(true);
        Time.timeScale = 0;
        SetIsPaused(true);
    }
    
    public void UnpauseGame()
    {
        GameplayPanel.SetActive(true);
        PauseScreen.SetActive(false);
        Time.timeScale = 1;
        SetIsPaused(false);
    }

    public bool GetIsPaused()
    {
        return isPaused;
    }

    public void SetIsPaused(bool value)
    {
        isPaused = value;
    }
}
