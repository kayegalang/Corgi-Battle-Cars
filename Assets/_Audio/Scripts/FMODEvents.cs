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

    [field: Header("Selection Click")]
    [field: SerializeField] public EventReference clicksound { get; private set; }
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
