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
        
        private Bus masterBus;
        private Bus musicBus;
        private Bus sfxBus;
        
        public static AudioManager instance { get; private set; }

        private void Awake()
        {
            if (instance == null)
                instance = this;
            else if (instance != this)
                Destroy(this);
            
            masterBus = RuntimeManager.GetBus("bus:/");
            musicBus  = RuntimeManager.GetBus("bus:/Music");
            sfxBus    = RuntimeManager.GetBus("bus:/SFX");
        }

        private void Update()
        {
            masterBus.setVolume(masterVolume);
            musicBus.setVolume(musicVolume);
            sfxBus.setVolume(sfxVolume);
        }

        public void PlayOneShot(EventReference sound, Vector3 worldPos)
        {
            EventInstance instance = RuntimeManager.CreateInstance(sound);
            instance.set3DAttributes(RuntimeUtils.To3DAttributes(worldPos));
            instance.start();
            instance.release();
        }

        /// <summary>Mute or unmute the SFX bus — called by EndGameManager during end screen.</summary>
        public void MuteSFX(bool mute)
        {
            sfxBus.setMute(mute);
        }
    }
}