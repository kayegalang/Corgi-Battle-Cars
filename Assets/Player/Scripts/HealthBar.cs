using UnityEngine;
using UnityEngine.UI;

namespace Player.Scripts
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
        }
        void Update()
        {
            if (carCamera != null) 
                transform.LookAt(carCamera.transform);
        }

        public void UpdateHealthBar()
        {
            healthBarSlider.value = health.GetHealthPercent(); 
        }
    }
}

