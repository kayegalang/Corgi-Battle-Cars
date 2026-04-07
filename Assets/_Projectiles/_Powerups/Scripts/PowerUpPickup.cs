using System.Collections.Generic;
using _Cars.Scripts;
using _PowerUps.ScriptableObjects;
using UnityEngine;
using UnityEngine.InputSystem;

namespace _PowerUps.Scripts
{
    public class PowerUpPickup : MonoBehaviour
    {
        [Header("Power Up Data")]
        [SerializeField] private PowerUpObject powerUp;
        
        [Header("Visuals")]
        [SerializeField] private GameObject    treatVisual;
        [SerializeField] private ParticleSystem smellTrailParticles;
        [SerializeField] private GameObject    collectEffectPrefab;
        
        [Header("Bobbing Animation")]
        [SerializeField] private float bobHeight   = 0.3f;
        [SerializeField] private float bobSpeed    = 2f;
        [SerializeField] private float rotateSpeed = 90f;
        
        [Header("Vibration Settings")]
        [SerializeField] private float maxVibrationStrength = 0.8f;
        
        private Vector3 startPosition;
        private bool    isCollected = false;
        
        private readonly List<GameObject>            playersInSmellRange     = new List<GameObject>();
        private readonly List<GameObject>            playersInVibrationRange = new List<GameObject>();
        private readonly List<GameObject>            playersInRevealRange    = new List<GameObject>();
        private readonly Dictionary<GameObject, bool> playerVibrating        = new Dictionary<GameObject, bool>();

        // ═══════════════════════════════════════════════
        //  LIFECYCLE
        // ═══════════════════════════════════════════════
        
        private void Start()
        {
            startPosition = transform.position;
            SetTreatVisible(false);
            
            if (smellTrailParticles != null && powerUp != null)
            {
                var main = smellTrailParticles.main;
                main.startColor = powerUp.smellTrailColor;
            }
        }
        
        private void Update()
        {
            if (isCollected) return;
            BobAndRotate();
            UpdateProximityEffects();
        }

        // ═══════════════════════════════════════════════
        //  VISUALS
        // ═══════════════════════════════════════════════
        
        private void BobAndRotate()
        {
            if (treatVisual == null || !treatVisual.activeSelf) return;
            
            float newY = startPosition.y + Mathf.Sin(Time.time * bobSpeed) * bobHeight;
            transform.position = new Vector3(transform.position.x, newY, transform.position.z);
            transform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime);
        }

        // ═══════════════════════════════════════════════
        //  PROXIMITY EFFECTS
        // ═══════════════════════════════════════════════
        
        private void UpdateProximityEffects()
        {
            var allPlayers = new List<GameObject>();
            string[] playerTags = { "PlayerOne", "PlayerTwo", "PlayerThree", "PlayerFour" };

            foreach (string tag in playerTags)
            {
                try { allPlayers.AddRange(GameObject.FindGameObjectsWithTag(tag)); }
                catch { }
            }

            foreach (GameObject player in allPlayers)
            {
                float distance = Vector3.Distance(transform.position, player.transform.position);
                HandleSmellRange(player, distance);
                HandleVibrationRange(player, distance);
                HandleRevealRange(player, distance);
            }
        }
        
        private void HandleSmellRange(GameObject player, float distance)
        {
            bool inRange    = distance <= powerUp.smellDistance;
            bool wasInRange = playersInSmellRange.Contains(player);
            
            if (inRange && !wasInRange)
            {
                playersInSmellRange.Add(player);
                OnPlayerEnterSmellRange(player);
            }
            else if (!inRange && wasInRange)
            {
                playersInSmellRange.Remove(player);
                OnPlayerExitSmellRange(player);
            }
        }
        
        private void OnPlayerEnterSmellRange(GameObject player)
        {
            if (smellTrailParticles != null && !smellTrailParticles.isPlaying)
                smellTrailParticles.Play();
        }
        
        private void OnPlayerExitSmellRange(GameObject player)
        {
            if (playersInSmellRange.Count == 0 && smellTrailParticles != null)
                smellTrailParticles.Stop();
        }
        
        private void HandleVibrationRange(GameObject player, float distance)
        {
            bool inRange    = distance <= powerUp.vibrationDistance;
            bool wasInRange = playersInVibrationRange.Contains(player);
            
            if (inRange && !wasInRange)
                playersInVibrationRange.Add(player);
            else if (!inRange && wasInRange)
            {
                playersInVibrationRange.Remove(player);
                StopVibration(player);
            }
            
            if (inRange)
                UpdateVibration(player, distance);
        }
        
        private void UpdateVibration(GameObject player, float distance)
        {
            float normalizedDistance = distance / powerUp.vibrationDistance;
            float strength = (1f - normalizedDistance) * maxVibrationStrength;
            
            if (distance <= powerUp.revealDistance)
                strength = maxVibrationStrength;
            
            PlayerInput playerInput = player.GetComponent<PlayerInput>();
            if (playerInput == null) return;
            
            foreach (var device in playerInput.devices)
            {
                if (device is Gamepad gamepad)
                {
                    gamepad.SetMotorSpeeds(strength * 0.5f, strength);
                    playerVibrating[player] = true;
                }
            }
        }
        
        private void StopVibration(GameObject player)
        {
            if (!playerVibrating.ContainsKey(player) || !playerVibrating[player]) return;
            
            PlayerInput playerInput = player.GetComponent<PlayerInput>();
            if (playerInput == null) return;
            
            foreach (var device in playerInput.devices)
                if (device is Gamepad gamepad)
                    gamepad.SetMotorSpeeds(0f, 0f);
            
            playerVibrating[player] = false;
        }
        
        private void HandleRevealRange(GameObject player, float distance)
        {
            bool inRange    = distance <= powerUp.revealDistance;
            bool wasInRange = playersInRevealRange.Contains(player);
            
            if (inRange && !wasInRange)
            {
                playersInRevealRange.Add(player);
                RevealTreat();
            }
            else if (!inRange && wasInRange)
            {
                playersInRevealRange.Remove(player);
                if (playersInRevealRange.Count == 0)
                    HideTreat();
            }
        }
        
        private void RevealTreat()
        {
            SetTreatVisible(true);
            Debug.Log($"[PowerUpPickup] {powerUp.powerUpName} treat revealed!");
        }
        
        private void HideTreat()       => SetTreatVisible(false);
        
        private void SetTreatVisible(bool visible)
        {
            if (treatVisual != null)
                treatVisual.SetActive(visible);
        }

        // ═══════════════════════════════════════════════
        //  COLLECTION
        // ═══════════════════════════════════════════════
        
        private void OnTriggerEnter(Collider other)
        {
            if (isCollected) return;
            
            // Walk up to root so we always get the player root even if collider is on a child
            GameObject root = other.transform.root.gameObject;

            if (!root.CompareTag("PlayerOne")   &&
                !root.CompareTag("PlayerTwo")   &&
                !root.CompareTag("PlayerThree") &&
                !root.CompareTag("PlayerFour"))
                return;
            
            Collect(root);
        }
        
        private void Collect(GameObject player)
        {
            // Don't collect if the player is dead
            CarHealth health = player.GetComponent<CarHealth>();
            if (health != null && health.IsDead()) return;

            PowerUpHandler handler = player.GetComponent<PowerUpHandler>();
            if (handler == null)
            {
                Debug.LogWarning($"[PowerUpPickup] {player.name} has no PowerUpHandler!");
                return;
            }
            
            bool pickedUp = handler.TryPickUpPowerUp(powerUp);
            
            if (!pickedUp)
            {
                Debug.Log($"[PowerUpPickup] {player.name} already has a power-up! Can't pick up another.");
                return;
            }
            
            isCollected = true;
            StopAllVibrations();
            
            if (powerUp.collectSound != null)
                AudioSource.PlayClipAtPoint(powerUp.collectSound, transform.position);
            
            if (collectEffectPrefab != null)
            {
                GameObject effect = Instantiate(collectEffectPrefab, transform.position, Quaternion.identity);
                Destroy(effect, 2f);
            }
            
            Debug.Log($"[PowerUpPickup] {player.name} collected {powerUp.powerUpName}!");
            if (gameObject != null) 
                Destroy(gameObject);
        }
        
        private void StopAllVibrations()
        {
            foreach (GameObject player in playersInVibrationRange)
                StopVibration(player);
        }
        
        private void OnDestroy()
        {
            StopAllVibrations();
        }

        // ═══════════════════════════════════════════════
        //  GIZMOS
        // ═══════════════════════════════════════════════
        
        private void OnDrawGizmosSelected()
        {
            if (powerUp == null) return;
            
            Gizmos.color = new Color(1f, 1f, 0f, 0.2f);
            Gizmos.DrawSphere(transform.position, powerUp.smellDistance);
            
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.2f);
            Gizmos.DrawSphere(transform.position, powerUp.vibrationDistance);
            
            Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
            Gizmos.DrawSphere(transform.position, powerUp.revealDistance);
        }
    }
}