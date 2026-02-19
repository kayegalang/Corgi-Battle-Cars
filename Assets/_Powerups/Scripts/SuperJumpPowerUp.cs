using _Bot.Scripts;
using _Cars.Scripts;
using UnityEngine;

namespace _PowerUps.ScriptableObjects
{
    [CreateAssetMenu(fileName = "SuperJump", menuName = "Power Ups/Super Jump")]
    public class SuperJumpPowerUp : PowerUpObject
    {
        [Header("Super Jump Settings")]
        [Tooltip("How much to multiply the car's jump force by")]
        [Range(1f, 10f)]
        public float jumpMultiplier = 3f;
        
        [Tooltip("How much to increase the jump height cap by")]
        [Range(1f, 5f)]
        public float jumpHeightCapMultiplier = 3f;
        
        public override void Apply(GameObject player)
        {
            // Works for both human players and bots
            CarController controller = player.GetComponent<CarController>();
            if (controller != null)
            {
                //controller.ApplyJumpMultiplier(jumpMultiplier, jumpHeightCapMultiplier);
                return;
            }
            
            BotController botController = player.GetComponent<BotController>();
            if (botController != null)
            {
                //botController.ApplyJumpMultiplier(jumpMultiplier, jumpHeightCapMultiplier);
                return;
            }
            
            Debug.LogWarning($"[SuperJumpPowerUp] No CarController or BotController found on {player.name}!");
        }
        
        public override void Remove(GameObject player)
        {
            if (player == null) return;
            
            CarController controller = player.GetComponent<CarController>();
            if (controller != null)
            {
                //controller.RemoveJumpMultiplier();
                return;
            }
            
            BotController botController = player.GetComponent<BotController>();
            if (botController != null)
            {
                //botController.RemoveJumpMultiplier();
            }
        }
    }
}
