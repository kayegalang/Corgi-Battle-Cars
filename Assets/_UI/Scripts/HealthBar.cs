using _Cars.Scripts;
using UnityEngine;
using UnityEngine.UI;

namespace _UI.Scripts
{
    public class HealthBar : MonoBehaviour
    {
        [SerializeField] private CarHealth health;
        private Slider healthBarSlider;

        void Start()
        {
            healthBarSlider = GetComponent<Slider>();
            healthBarSlider.value = 1;
        }

        public void UpdateHealthBar()
        {
            if (healthBarSlider != null)
                healthBarSlider.value = health.GetHealthPercent(); 
        }
    }
}