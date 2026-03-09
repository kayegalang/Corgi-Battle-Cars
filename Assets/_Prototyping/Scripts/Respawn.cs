using _Gameplay.Scripts;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _Prototyping.Scripts
{
    public class Respawn : MonoBehaviour
    {
        [SerializeField] SpawnManager spawnManager;
        public void OnClick()
        {
           spawnManager.Respawn("PlayerOne");
        }
    }
}

