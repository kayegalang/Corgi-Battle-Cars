using UnityEngine;
using FMODUnity;

namespace _Audio.scripts 
{
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager instance { get; private set; }

        private void Awake()
        {
            if (instance == null)
                instance = this;
            else if (instance != this)
            {
                Debug.LogWarning($"[MatchTimerManager] Duplicate found — destroying this one on {gameObject.name}!");
                Destroy(this);
            }
        }

        public void PlayOneShot(EventReference sound, Vector3 worldPos)
        {
            RuntimeManager.PlayOneShot(sound,worldPos);
        }
    }
}
