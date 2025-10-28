using UnityEngine;

public class StartScreenController : MonoBehaviour
{
    [SerializeField] private GameObject[] scenePanels;

    void Start()
    {
        gameObject.SetActive(true);
        foreach (GameObject panel in scenePanels)
        {
            panel.SetActive(false);
        }
    }
}
