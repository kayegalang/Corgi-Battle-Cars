using UnityEngine;
using FMODUnity;

namespace _Audio.scripts 
{
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager instance { get; private set; }

        private void Awake()
        {
            if (instance != null)
            {
                Debug.LogError("Shiver me timbers! More than one Audio Manager was found in the scene.");
            }
            instance = this;
        }

        public void PlayOneShot(EventReference sound, Vector3 worldPos)
        {
            RuntimeManager.PlayOneShot(sound,worldPos);
        }
    }
}
