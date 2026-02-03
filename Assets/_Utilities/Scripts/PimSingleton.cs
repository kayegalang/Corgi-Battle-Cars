using UnityEngine;
using UnityEngine.InputSystem;

namespace _Utilities.Scripts
{
    public class PimSingleton : MonoBehaviour
    {
        private static PimSingleton instance;
        private void Awake()
        {
            InitializeSingleton();
        }
        
        private void InitializeSingleton()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);

            }
            else if (instance != this)
            {
                Destroy(this);
            }
        }

    }
}
