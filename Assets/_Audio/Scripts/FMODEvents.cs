using UnityEngine;
using FMODUnity;
using UnityEngine.Rendering;

public class FMODEvents : MonoBehaviour
{
    
    // [field: Header("Dog Park Music")]
    // [field: SerializeField] public EventRefernce DogParkStart { get; private set; }
    
    [field: Header("Countdown Barks")]
    [field: SerializeField] public EventReference bark3sound { get; private set; }
    [field: SerializeField] public EventReference bark2sound { get; private set; } 
    [field: SerializeField] public EventReference bark1sound { get; private set; }

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
