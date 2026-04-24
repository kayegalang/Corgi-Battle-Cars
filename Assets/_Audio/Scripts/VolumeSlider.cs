using UnityEngine;
using UnityEngine.UI;

namespace _Audio.scripts
{
    public class VolumeSlider : MonoBehaviour
    {
        private enum VolumeType
        {
            MASTER,
            MUSIC,
            SFX,
            ANNOUNCER,
        }

        [Header("Type")]
        [SerializeField] private VolumeType volumeType;

        private Slider volumeSlider;

        private void Awake()
        {
            volumeSlider = GetComponent<Slider>();
        }

        private void Start()
        {
            switch (volumeType)
            {
                case VolumeType.MASTER:
                    volumeSlider.SetValueWithoutNotify(AudioManager.instance.masterVolume);
                    break;
                case VolumeType.ANNOUNCER:
                    volumeSlider.SetValueWithoutNotify(AudioManager.instance.musicVolume);
                    break;
                case VolumeType.MUSIC:
                    volumeSlider.SetValueWithoutNotify(AudioManager.instance.musicVolume);
                    break;
                case VolumeType.SFX:
                    volumeSlider.SetValueWithoutNotify(AudioManager.instance.sfxVolume);
                    break;
            }
        }

        public void OnSliderValueChanged()
        {
            switch (volumeType)
            {
                case VolumeType.MASTER:
                    AudioManager.instance.masterVolume = volumeSlider.value;
                    break;
                case VolumeType.ANNOUNCER:
                    AudioManager.instance.musicVolume = volumeSlider.value;
                    break;
                case VolumeType.MUSIC:
                    AudioManager.instance.musicVolume = volumeSlider.value;
                    break;
                case VolumeType.SFX:
                    AudioManager.instance.sfxVolume = volumeSlider.value;
                    break;
            }
        }
    }
}