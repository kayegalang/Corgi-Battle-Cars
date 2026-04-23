using System.Collections.Generic;
using UnityEngine;
using FMODUnity;
using FMOD.Studio;

namespace _Audio.scripts 
{
    public class AudioManager : MonoBehaviour
    {
        [Header("Volume")] 
        [Range(0, 1)] 
        public float masterVolume = 1;
        [Range(0, 1)]
        public float musicVolume = 1;
        [Range(0, 1)]
        public float sfxVolume = 1;
        [Range(0, 1)] 
        
        private Bus masterBus;
        private Bus musicBus;
        private Bus sfxBus;
        
        public static AudioManager instance { get; private set; }

        private void Awake()
        {
            if (instance != null)
            {
                Debug.LogError("Shiver me timbers! More than one Audio Manager was found in the scene.");
                return;
            }
            instance = this;
            
            masterBus = RuntimeManager.GetBus("bus:/");
            musicBus = RuntimeManager.GetBus("bus:/Music");
            sfxBus = RuntimeManager.GetBus("bus:/SFX");
        }

        private void Update()
        {
            masterBus.setVolume(masterVolume);
            musicBus.setVolume(musicVolume);
            sfxBus.setVolume(sfxVolume);
        }

        public void PlayOneShot(EventReference sound, Vector3 worldPos)
        {
            RuntimeManager.PlayOneShot(sound,worldPos);
        }
    }
}
