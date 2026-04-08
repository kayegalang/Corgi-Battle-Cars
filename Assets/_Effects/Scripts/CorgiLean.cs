using UnityEngine;
using _Bot.Scripts;

namespace _Cars.Scripts
{
    /// <summary>
    /// Tilts the corgi mesh on Z when turning to simulate car body roll.
    /// Add to the corgi mesh GameObject.
    /// </summary>
    public class CorgiLean : MonoBehaviour
    {
        [Header("Lean Settings")]
        [SerializeField] private float maxLeanAngle = 15f;
        [SerializeField] private float leanSpeed    = 5f;

        private CarController carController;
        private BotController botController;
        private float         currentLean = 0f;

        private void Awake()
        {
            carController = GetComponentInParent<CarController>();
            botController = GetComponentInParent<BotController>();
        }

        private void Update()
        {
            float steerInput = 0f;
            if (carController != null)      steerInput = carController.GetMoveInput().x;
            else if (botController != null) steerInput = botController.GetMoveInput().x;

            float targetLean  = steerInput * maxLeanAngle;
            currentLean       = Mathf.Lerp(currentLean, targetLean, leanSpeed * Time.deltaTime);

            Vector3 euler     = transform.localEulerAngles;
            euler.z           = currentLean;
            transform.localEulerAngles = euler;
        }
    }
}