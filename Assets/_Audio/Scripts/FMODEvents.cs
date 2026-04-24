using UnityEngine;
using FMODUnity;
using UnityEngine.Rendering;

namespace _Audio.scripts
{
    public class FMODEvents : MonoBehaviour
    {
    
        [field: Header("Music")]
        [field: SerializeField] public EventReference DogParkStart { get; private set; }
        [field: SerializeField] public EventReference MainMenuMusic { get; private set; }
    
        [field: Header("Countdown Barks + Finished")]
        [field: SerializeField] public EventReference bark3Sound { get; private set; }
        [field: SerializeField] public EventReference bark2Sound { get; private set; } 
        [field: SerializeField] public EventReference bark1Sound { get; private set; }
        [field: SerializeField] public EventReference barkGoSound { get; private set; }
        [field: SerializeField] public EventReference FinishedSound { get; private set; }

        [field: Header("UI")]
        [field: SerializeField] public EventReference clicksound { get; private set; }
        [field: SerializeField] public EventReference joinbark { get; private set; }
    
        [field: Header("PowerUps")]
        [field: SerializeField] public EventReference shootsound { get; private set; }
        [field: SerializeField] public EventReference zoomies { get; private set; }
        [field: SerializeField] public EventReference sniff { get; private set; }
        [field: SerializeField] public EventReference squirrel { get; private set; }
        [field: SerializeField] public EventReference poop { get; private set; }
        [field: SerializeField] public EventReference jump { get; private set; }
        [field: SerializeField] public EventReference superjump { get; private set; }
        [field: SerializeField] public EventReference laserbeam { get; private set; }
        
        [field: Header("Other SFX")]
        [field: SerializeField] public EventReference hit { get; private set; }
        [field: SerializeField] public EventReference honkbark { get; private set; }
        [field: SerializeField] public EventReference explosion { get; private set; }
        [field: SerializeField] public EventReference carengine { get; private set; }
        [field: SerializeField] public EventReference carenginelowhealth { get; private set; }
        
        public static FMODEvents instance { get; private set; }

        private void Awake()
        {
            if (instance != null)
            {
                Debug.LogError("Thar be more than FMODEvents in this here scene!");
            }

            instance = this;
        }
    }
}