using UnityEngine;
using UnityEngine.EventSystems;
using _Audio.scripts;

namespace _UI.Scripts
{
    /// <summary>
    /// Add to any Button to play FMOD click sounds on hover and click.
    /// Uses FMODEvents.instance.clicksound for both.
    /// </summary>
    public class ButtonSoundEffect : MonoBehaviour,
        IPointerEnterHandler,
        IPointerClickHandler,
        ISelectHandler      // fires when button is highlighted via keyboard/controller
    {
        [Header("Optional Overrides — leave unchecked to use default click sound")]
        [SerializeField] private bool useCustomHoverSound = false;
        [SerializeField] private bool useCustomClickSound = false;
        [SerializeField] private FMODUnity.EventReference customHoverSound;
        [SerializeField] private FMODUnity.EventReference customClickSound;

        // ═══════════════════════════════════════════════
        //  HOVER — mouse enters OR controller navigates to button
        // ═══════════════════════════════════════════════

        public void OnPointerEnter(PointerEventData eventData)
        {
            PlayHover();
        }

        public void OnSelect(BaseEventData eventData)
        {
            PlayHover();
        }

        // ═══════════════════════════════════════════════
        //  CLICK
        // ═══════════════════════════════════════════════

        public void OnPointerClick(PointerEventData eventData)
        {
            PlayClick();
        }

        // ═══════════════════════════════════════════════
        //  PLAY
        // ═══════════════════════════════════════════════

        private void PlayHover()
        {
            if (FMODEvents.instance == null || AudioManager.instance == null) return;

            var sound = useCustomHoverSound ? customHoverSound : FMODEvents.instance.clicksound;
            AudioManager.instance.PlayOneShot(sound, transform.position);
        }

        private void PlayClick()
        {
            if (FMODEvents.instance == null || AudioManager.instance == null) return;

            var sound = useCustomClickSound ? customClickSound : FMODEvents.instance.clicksound;
            AudioManager.instance.PlayOneShot(sound, transform.position);
        }
    }
}
