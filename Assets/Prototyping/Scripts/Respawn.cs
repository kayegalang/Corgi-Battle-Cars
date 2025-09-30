using UnityEngine;
using UnityEngine.SceneManagement;

namespace Prototyping.Scripts
{
    public class Respawn : MonoBehaviour
    {
        public void OnClick()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}

