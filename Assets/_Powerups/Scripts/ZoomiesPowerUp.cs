using _Bot.Scripts;
using _Cars.Scripts;
using UnityEngine;

namespace _PowerUps.ScriptableObjects
{
    [CreateAssetMenu(fileName = "Zoomies", menuName = "Power Ups/Zoomies")]
    public class ZoomiesPowerUp : PowerUpObject
    {
        [Header("Zoomies Settings")]
        [Tooltip("How much to multiply the car's max speed by")]
        [Range(1f, 5f)]
        public float speedMultiplier = 2.5f;
        
        [Tooltip("How much to multiply the car's acceleration by")]
        [Range(1f, 5f)]
        public float accelerationMultiplier = 2f;
        
        public override void Apply(GameObject player)
        {
            // Works for both human players and bots
            CarController controller = player.GetComponent<CarController>();
            if (controller != null)
            {
                //controller.ApplySpeedMultiplier(speedMultiplier, accelerationMultiplier);
                return;
            }
            
            BotController botController = player.GetComponent<BotController>();
            if (botController != null)
            {
                //botController.ApplySpeedMultiplier(speedMultiplier, accelerationMultiplier);
                return;
            }
            
            Debug.LogWarning($"[ZoomiesPowerUp] No CarController or BotController found on {player.name}!");
        }
        
        public override void Remove(GameObject player)
        {
            if (player == null) return;
            
            CarController controller = player.GetComponent<CarController>();
            if (controller != null)
            {
                //controller.RemoveSpeedMultiplier();
                return;
            }
            
            BotController botController = player.GetComponent<BotController>();
            if (botController != null)
            {
                //botController.RemoveSpeedMultiplier();
            }
        }
    }
}
