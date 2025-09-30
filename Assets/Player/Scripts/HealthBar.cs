using UnityEngine;
using UnityEngine.UI;

namespace Player.Scripts
{
    public class HealthBar : MonoBehaviour
    {
        private CarHealth health;
        private Image fillImage;

        void Update()
        {
            fillImage.fillAmount = health.GetHealthPercent();
            transform.LookAt(gameObject.GetComponent<Camera>().transform); 
        }
    }
}

