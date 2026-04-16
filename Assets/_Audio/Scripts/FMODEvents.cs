using UnityEngine;
using FMODUnity;
using UnityEngine.Rendering;

public class FMODEvents : MonoBehaviour
{
    
    [field: Header("Dog Park Music")]
    [field: SerializeField] public EventReference DogParkStart { get; private set; }
    
    [field: Header("Countdown Barks")]
    [field: SerializeField] public EventReference bark3Sound { get; private set; }
    [field: SerializeField] public EventReference bark2Sound { get; private set; } 
    [field: SerializeField] public EventReference bark1Sound { get; private set; }
    [field: SerializeField] public EventReference barkGoSound { get; private set; }

    [field: Header("UI")]
    [field: SerializeField] public EventReference clicksound { get; private set; }
    
    [field: Header("PowerUps")]
    [field: SerializeField] public EventReference shootsound { get; private set; }
    public static FMODEvents instance { get; private set; }

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
}
