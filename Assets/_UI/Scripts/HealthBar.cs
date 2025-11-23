using _Cars.Scripts;
using UnityEngine;
using UnityEngine.UI;

namespace _UI.Scripts
{
    public class HealthBar : MonoBehaviour
    {
        [SerializeField] private CarHealth health;
        [SerializeField] private Camera carCamera;
        private Slider healthBarSlider;

        void Start()
        {
            healthBarSlider = GetComponent<Slider>();
            healthBarSlider.value = 1;
            
            // Get the camera from this player's hierarchy, NOT Camera.main
            if (carCamera == null)
            {
                carCamera = GetComponentInParent<Camera>();
                if (carCamera == null)
                {
                    // Search in siblings
                    Transform parent = transform.parent?.parent; // Go up to player root
                    if (parent != null)
                    {
                        carCamera = parent.GetComponentInChildren<Camera>();
                    }
                }
            }
        }
        
        void Update()
        {
            if (carCamera != null)
            {
                transform.LookAt(carCamera.transform);
            }
        }

        public void UpdateHealthBar()
        {
            healthBarSlider.value = health.GetHealthPercent(); 
        }
    }
}