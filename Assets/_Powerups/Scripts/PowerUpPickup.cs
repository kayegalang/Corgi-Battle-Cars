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
        [SerializeField] private GameObject     treatVisual;
        [SerializeField] private ParticleSystem smellTrailParticles;
        [SerializeField] private GameObject     collectEffectPrefab;
        
        [Header("Bobbing Animation")]
        [SerializeField] private float bobHeight   = 0.3f;
        [SerializeField] private float bobSpeed    = 2f;
        [SerializeField] private float rotateSpeed = 90f;
        
        [Header("Vibration Settings")]
        [SerializeField] private float maxVibrationStrength = 0.8f;
        
        private Vector3 startPosition;
        private bool    isCollected     = false;
        private bool    isCinematicMode = false;
        
        // Track proximity for both players and bots
        private readonly List<GameObject>             allInSmellRange     = new List<GameObject>();
        private readonly List<GameObject>             allInVibrationRange = new List<GameObject>();
        private readonly List<GameObject>             allInRevealRange    = new List<GameObject>();
        private readonly Dictionary<GameObject, bool> playerVibrating     = new Dictionary<GameObject, bool>();

        private static readonly string[] PlayerTags = { "PlayerOne", "PlayerTwo", "PlayerThree", "PlayerFour" };
        private static readonly string[] BotTags    = { "BotOne", "BotTwo", "BotThree", "BotFour" };

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

            if (!isCinematicMode)
                UpdateProximityEffects();
        }

        // ═══════════════════════════════════════════════
        //  CINEMATIC MODE
        // ═══════════════════════════════════════════════

        public void SetCinematicVisible(bool visible)
        {
            isCinematicMode = visible;
            SetTreatVisible(visible);

            if (smellTrailParticles != null)
            {
                if (visible) smellTrailParticles.Play();
                else         smellTrailParticles.Stop();
            }
        }

        // ═══════════════════════════════════════════════
        //  PUBLIC API — for BotAI to query
        // ═══════════════════════════════════════════════

        /// <summary>
        /// Returns true if this pickup is within smell range of the given position.
        /// BotAI uses this to "sniff" for nearby power-ups.
        /// </summary>
        public bool IsWithinSmellRange(Vector3 position)
        {
            if (powerUp == null) return false;
            return Vector3.Distance(transform.position, position) <= powerUp.smellDistance;
        }

        /// <summary>
        /// Returns true if this pickup is within reveal range of the given position.
        /// </summary>
        public bool IsWithinRevealRange(Vector3 position)
        {
            if (powerUp == null) return false;
            return Vector3.Distance(transform.position, position) <= powerUp.revealDistance;
        }

        public PowerUpObject GetPowerUp() => powerUp;

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
        //  PROXIMITY EFFECTS — players AND bots
        // ═══════════════════════════════════════════════
        
        private void UpdateProximityEffects()
        {
            var allEntities = new List<GameObject>();

            // Collect players
            foreach (string tag in PlayerTags)
            {
                try { allEntities.AddRange(GameObject.FindGameObjectsWithTag(tag)); }
                catch { }
            }

            // Collect bots
            foreach (string tag in BotTags)
            {
                try { allEntities.AddRange(GameObject.FindGameObjectsWithTag(tag)); }
                catch { }
            }

            foreach (GameObject entity in allEntities)
            {
                float distance = Vector3.Distance(transform.position, entity.transform.position);
                HandleSmellRange(entity, distance);
                HandleRevealRange(entity, distance);

                // Only vibrate human players
                if (IsPlayer(entity))
                    HandleVibrationRange(entity, distance);
            }
        }

        private bool IsPlayer(GameObject entity)
        {
            foreach (string tag in PlayerTags)
                if (entity.CompareTag(tag)) return true;
            return false;
        }

        private bool IsBot(GameObject entity)
        {
            foreach (string tag in BotTags)
                if (entity.CompareTag(tag)) return true;
            return false;
        }
        
        private void HandleSmellRange(GameObject entity, float distance)
        {
            bool inRange    = distance <= powerUp.smellDistance;
            bool wasInRange = allInSmellRange.Contains(entity);
            
            if (inRange && !wasInRange)
            {
                allInSmellRange.Add(entity);
                OnEntityEnterSmellRange();
            }
            else if (!inRange && wasInRange)
            {
                allInSmellRange.Remove(entity);
                OnEntityExitSmellRange();
            }
        }
        
        private void OnEntityEnterSmellRange()
        {
            if (smellTrailParticles != null && !smellTrailParticles.isPlaying)
                smellTrailParticles.Play();
        }
        
        private void OnEntityExitSmellRange()
        {
            if (allInSmellRange.Count == 0 && smellTrailParticles != null)
                smellTrailParticles.Stop();
        }
        
        private void HandleVibrationRange(GameObject player, float distance)
        {
            bool inRange    = distance <= powerUp.vibrationDistance;
            bool wasInRange = allInVibrationRange.Contains(player);
            
            if (inRange && !wasInRange)
                allInVibrationRange.Add(player);
            else if (!inRange && wasInRange)
            {
                allInVibrationRange.Remove(player);
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
        
        private void HandleRevealRange(GameObject entity, float distance)
        {
            bool inRange    = distance <= powerUp.revealDistance;
            bool wasInRange = allInRevealRange.Contains(entity);
            
            if (inRange && !wasInRange)
            {
                allInRevealRange.Add(entity);
                RevealTreat();
            }
            else if (!inRange && wasInRange)
            {
                allInRevealRange.Remove(entity);
                if (allInRevealRange.Count == 0)
                    HideTreat();
            }
        }
        
        private void RevealTreat()   => SetTreatVisible(true);
        private void HideTreat()     => SetTreatVisible(false);
        
        private void SetTreatVisible(bool visible)
        {
            if (treatVisual != null)
                treatVisual.SetActive(visible);
        }

        // ═══════════════════════════════════════════════
        //  COLLECTION — players AND bots
        // ═══════════════════════════════════════════════
        
        private void OnTriggerEnter(Collider other)
        {
            if (isCollected) return;
            
            GameObject root = other.transform.root.gameObject;

            bool isPlayer = IsPlayer(root);
            bool isBot    = IsBot(root);

            if (!isPlayer && !isBot) return;
            
            Collect(root, isBot);
        }
        
        private void Collect(GameObject collector, bool isBot)
        {
            // Don't collect if dead
            CarHealth health = collector.GetComponent<CarHealth>();
            if (health != null && health.IsDead()) return;

            PowerUpHandler handler = collector.GetComponent<PowerUpHandler>();
            if (handler == null)
            {
                Debug.LogWarning($"[PowerUpPickup] {collector.name} has no PowerUpHandler!");
                return;
            }
            
            bool pickedUp = handler.TryPickUpPowerUp(powerUp);
            
            if (!pickedUp)
            {
                Debug.Log($"[PowerUpPickup] {collector.name} already has a power-up!");
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
            
            Debug.Log($"[PowerUpPickup] {collector.name} collected {powerUp.powerUpName}!");
            if (gameObject != null) 
                Destroy(gameObject);
        }
        
        private void StopAllVibrations()
        {
            foreach (GameObject player in allInVibrationRange)
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