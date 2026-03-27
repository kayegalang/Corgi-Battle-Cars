using System.Collections;
using System.Collections.Generic;
using _Cars.Scripts;
using _UI.Scripts;
using _Player.Scripts;
using UnityEngine;
using UnityEngine.InputSystem;

namespace _Gameplay.Scripts 
{
    public class SpawnManager : MonoBehaviour
    {
        [SerializeField] private Transform[] spawnPoints;
        [SerializeField] private GameObject  playerPrefab;
        [SerializeField] private GameObject  botPrefab;
        
        [Header("Spawn Settings")]
        [SerializeField] private float respawnDelay = 3f;
        
        private readonly HashSet<int> usedSpawnIndices = new HashSet<int>();
        
        private const int    TOTAL_PLAYERS     = 4;
        private const string PLAYER_TAG_PREFIX = "Player";
        private const string BOT_TAG_PREFIX    = "Bot";
        
        private static readonly Dictionary<int, string> PlayerTagMap = new Dictionary<int, string>
        {
            { 1, "PlayerOne"   },
            { 2, "PlayerTwo"   },
            { 3, "PlayerThree" },
            { 4, "PlayerFour"  }
        };
        
        private static readonly Dictionary<int, string> BotTagMap = new Dictionary<int, string>
        {
            { 1, "BotOne"   },
            { 2, "BotTwo"   },
            { 3, "BotThree" },
            { 4, "BotFour"  }
        };

        // ═══════════════════════════════════════════════
        //  UNITY LIFECYCLE
        // ═══════════════════════════════════════════════
        
        private void Awake()
        {
            ClearUsedSpawnIndices();
            ValidateReferences();
        }
        
        private void ClearUsedSpawnIndices()
        {
            usedSpawnIndices.Clear();
        }
        
        private void ValidateReferences()
        {
            if (spawnPoints == null || spawnPoints.Length == 0)
                Debug.LogError($"[{nameof(SpawnManager)}] No spawn points assigned!");
            
            if (playerPrefab == null)
                Debug.LogError($"[{nameof(SpawnManager)}] Player prefab is not assigned!");
            
            if (botPrefab == null)
                Debug.LogError($"[{nameof(SpawnManager)}] Bot prefab is not assigned!");
        }

        // ═══════════════════════════════════════════════
        //  START GAME — no spawn protection on initial spawn
        // ═══════════════════════════════════════════════
        
        public void StartGame(int humanPlayerCount)
        {
            if (!ValidatePlayerCount(humanPlayerCount)) return;
            
            Debug.Log($"[{nameof(SpawnManager)}] Starting game with {humanPlayerCount} human players");
            
            SpawnHumanPlayers(humanPlayerCount);
            SpawnBots(humanPlayerCount);
        }
        
        private bool ValidatePlayerCount(int humanPlayerCount)
        {
            if (humanPlayerCount < 0 || humanPlayerCount > TOTAL_PLAYERS)
            {
                Debug.LogError($"[{nameof(SpawnManager)}] Invalid player count: {humanPlayerCount}");
                return false;
            }
            return true;
        }
        
        private void SpawnHumanPlayers(int humanPlayerCount)
        {
            for (int i = 1; i <= humanPlayerCount; i++)
            {
                string    playerTag  = GetPlayerTag(i);
                Transform spawnPoint = GetUniqueSpawnPoint();
                
                // Initial spawn — no spawn protection
                GameObject player = SpawnPlayer(playerTag, spawnPoint, playerPrefab);
                
                if (player != null)
                    RestorePlayerDevice(player, i);
                
                RegisterPlayer(playerTag);
            }
        }
        
        private void SpawnBots(int humanPlayerCount)
        {
            int botsNeeded = TOTAL_PLAYERS - humanPlayerCount;
            
            for (int i = 0; i < botsNeeded; i++)
            {
                int    botNumber = humanPlayerCount + i + 1;
                string botTag    = GetBotTag(botNumber);
                
                SpawnBot(botTag, GetUniqueSpawnPoint());
                RegisterPlayer(botTag);
            }
        }
        
        private void RegisterPlayer(string playerTag)
        {
            if (GameplayManager.instance != null)
                GameplayManager.instance.UpdatePlayerList(playerTag);
        }

        // ═══════════════════════════════════════════════
        //  RESPAWN — spawn protection activated here
        // ═══════════════════════════════════════════════
        
        public void Respawn(string playerTag)
        {
            GameObject prefabToSpawn = GetPrefabForTag(playerTag);
            
            if (prefabToSpawn == null)
            {
                Debug.LogError($"[{nameof(SpawnManager)}] Cannot respawn - no prefab for tag: {playerTag}");
                return;
            }
            
            StartCoroutine(RespawnAfterDelay(playerTag, prefabToSpawn));
        }
        
        private GameObject GetPrefabForTag(string playerTag)
        {
            if (playerTag.StartsWith(BOT_TAG_PREFIX))    return botPrefab;
            if (playerTag.StartsWith(PLAYER_TAG_PREFIX)) return playerPrefab;
            return null;
        }
        
        private IEnumerator RespawnAfterDelay(string playerTag, GameObject prefab)
        {
            yield return new WaitForSeconds(respawnDelay);
            
            Transform spawnPoint = GetFurthestSpawnPoint();
            
            if (spawnPoint == null)
            {
                Debug.LogError($"[{nameof(SpawnManager)}] Cannot respawn - no spawn point available!");
                yield break;
            }
            
            bool isBot = playerTag.StartsWith(BOT_TAG_PREFIX);
            
            if (isBot)
            {
                SpawnBot(playerTag, spawnPoint);
            }
            else
            {
                GameObject player = SpawnPlayer(playerTag, spawnPoint, prefab);
                
                if (player != null)
                {
                    int playerNumber = GetPlayerNumberFromTag(playerTag);
                    RestorePlayerDevice(player, playerNumber);

                    // Activate spawn protection ONLY on respawn, not initial spawn
                    player.GetComponent<CarHealth>()?.ActivateSpawnProtection();
                }
            }
            
            EnableGameplayForRespawnedPlayer(playerTag);
        }

        // ═══════════════════════════════════════════════
        //  SPAWN HELPERS
        // ═══════════════════════════════════════════════
        
        private GameObject SpawnPlayer(string playerTag, Transform spawnPoint, GameObject prefab)
        {
            PlayerInput prefabInput    = prefab.GetComponent<PlayerInput>();
            bool        hasPlayerInput = prefabInput != null;
            bool        wasEnabled     = false;
            
            if (hasPlayerInput)
            {
                wasEnabled          = prefabInput.enabled;
                prefabInput.enabled = false;
            }
            
            GameObject player = Instantiate(prefab, spawnPoint.position, spawnPoint.rotation);

            // Set identity FIRST so loaders can read the correct tag
            SetPlayerIdentity(player, playerTag);

            // Load stats — tag is correct so per-player keys work
            player.GetComponent<CarStatsLoader>()?.LoadForCurrentTag();
            player.GetComponent<WeaponStatsLoader>()?.LoadForCurrentTag();
            
            if (hasPlayerInput)
                RestorePlayerInput(player, prefabInput, wasEnabled);
            
            return player;
        }
        
        private void SpawnBot(string botTag, Transform spawnPoint)
        {
            GameObject bot = Instantiate(botPrefab, spawnPoint.position, spawnPoint.rotation);
            SetPlayerIdentity(bot, botTag);
        }
        
        private void SetPlayerIdentity(GameObject player, string playerTag)
        {
            player.tag  = playerTag;
            player.name = playerTag;
        }

        // ═══════════════════════════════════════════════
        //  SPAWN POINT SELECTION
        // ═══════════════════════════════════════════════

        private Transform GetUniqueSpawnPoint()
        {
            if (spawnPoints.Length == 0)
            {
                Debug.LogError($"[{nameof(SpawnManager)}] No spawn points available!");
                return null;
            }
            
            if (AllSpawnPointsUsed())
                ClearUsedSpawnIndices();
            
            int randomIndex = FindUnusedSpawnIndex();
            usedSpawnIndices.Add(randomIndex);
            
            return spawnPoints[randomIndex];
        }

        private Transform GetFurthestSpawnPoint()
        {
            if (spawnPoints.Length == 0)
            {
                Debug.LogError($"[{nameof(SpawnManager)}] No spawn points available!");
                return null;
            }

            var allCars = FindObjectsByType<CarHealth>(FindObjectsSortMode.None);

            if (allCars.Length == 0)
                return spawnPoints[Random.Range(0, spawnPoints.Length)];

            Transform bestPoint = spawnPoints[0];
            float     bestScore = -1f;

            foreach (var point in spawnPoints)
            {
                float totalDistance = 0f;
                foreach (var car in allCars)
                {
                    if (car == null) continue;
                    totalDistance += Vector3.Distance(point.position, car.transform.position);
                }

                if (totalDistance > bestScore)
                {
                    bestScore = totalDistance;
                    bestPoint = point;
                }
            }

            return bestPoint;
        }

        public Transform GetRespawnPoint() => GetFurthestSpawnPoint();

        private bool AllSpawnPointsUsed() => usedSpawnIndices.Count >= spawnPoints.Length;
        
        private int FindUnusedSpawnIndex()
        {
            int randomIndex;
            do
            {
                randomIndex = Random.Range(0, spawnPoints.Length);
            } 
            while (usedSpawnIndices.Contains(randomIndex));
            return randomIndex;
        }

        // ═══════════════════════════════════════════════
        //  TAG HELPERS
        // ═══════════════════════════════════════════════
        
        private string GetPlayerTag(int playerNumber)
        {
            if (PlayerTagMap.TryGetValue(playerNumber, out string tag))
                return tag;
            
            Debug.LogWarning($"[{nameof(SpawnManager)}] Invalid player number: {playerNumber}");
            return "PlayerOne";
        }
        
        private int GetPlayerNumberFromTag(string playerTag)
        {
            foreach (var kvp in PlayerTagMap)
                if (kvp.Value == playerTag) return kvp.Key;
            
            Debug.LogWarning($"[{nameof(SpawnManager)}] Unknown player tag: {playerTag}");
            return 1;
        }
        
        private string GetBotTag(int botPosition)
        {
            if (BotTagMap.TryGetValue(botPosition, out string tag))
                return tag;
            
            Debug.LogWarning($"[{nameof(SpawnManager)}] Invalid bot position: {botPosition}");
            return "BotOne";
        }

        // ═══════════════════════════════════════════════
        //  DEVICE RESTORATION
        // ═══════════════════════════════════════════════
        
        private void RestorePlayerDevice(GameObject player, int playerNumber)
        {
            PlayerInput playerInput = player.GetComponent<PlayerInput>();
            
            if (playerInput == null)
            {
                Debug.LogWarning($"[{nameof(SpawnManager)}] No PlayerInput on Player {playerNumber}!");
                return;
            }
            
            string      playerTag = GetPlayerTag(playerNumber);
            InputDevice device    = PlayerDeviceTracker.instance?.GetPlayerDevice(playerTag);
            
            if (device == null)
            {
                Debug.LogWarning($"[{nameof(SpawnManager)}] No device tracked for {playerTag}. Using defaults.");
                
                if (playerNumber == 1)
                    SetPlayerOneControlScheme(player);
                else
                    UseAnyAvailableGamepad(playerInput, playerNumber);
                return;
            }
            
            string controlScheme = device is Gamepad ? "Controller" : "Keyboard";
            Debug.Log($"[{nameof(SpawnManager)}] Restoring {playerTag} → {device.displayName} ({controlScheme})");
            
            try
            {
                if (device is Keyboard && Mouse.current != null)
                    playerInput.SwitchCurrentControlScheme(controlScheme, device, Mouse.current);
                else
                    playerInput.SwitchCurrentControlScheme(controlScheme, device);
                
                Debug.Log($"[{nameof(SpawnManager)}] ✓ {playerTag} restored with {device.displayName}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[{nameof(SpawnManager)}] Failed to restore device for {playerTag}: {e.Message}");
            }
        }
        
        private void UseAnyAvailableGamepad(PlayerInput playerInput, int playerNumber)
        {
            if (Gamepad.all.Count >= playerNumber)
            {
                Gamepad gamepad = Gamepad.all[playerNumber - 1];
                playerInput.SwitchCurrentControlScheme("Controller", gamepad);
                Debug.Log($"[{nameof(SpawnManager)}] Player {playerNumber} using fallback gamepad: {gamepad.displayName}");
            }
            else
            {
                Debug.LogWarning($"[{nameof(SpawnManager)}] Not enough gamepads for Player {playerNumber}");
            }
        }
        
        private void SetPlayerOneControlScheme(GameObject player)
        {
            PlayerInput playerInput = player.GetComponent<PlayerInput>();
            
            if (playerInput == null)
            {
                Debug.LogWarning($"[{nameof(SpawnManager)}] No PlayerInput on PlayerOne!");
                return;
            }
            
            bool shouldUseController = PlayerOneInputTracker.instance != null && 
                                       PlayerOneInputTracker.instance.IsPlayerOneUsingController();
            
            if (shouldUseController)
            {
                if (Gamepad.current != null)
                {
                    playerInput.SwitchCurrentControlScheme("Controller", Gamepad.current);
                    Debug.Log($"[{nameof(SpawnManager)}] ✓ PlayerOne using Controller");
                }
                else
                {
                    Debug.LogWarning($"[{nameof(SpawnManager)}] No gamepad found!");
                }
            }
            else
            {
                if (Keyboard.current != null && Mouse.current != null)
                {
                    playerInput.SwitchCurrentControlScheme("Keyboard", Keyboard.current, Mouse.current);
                    Debug.Log($"[{nameof(SpawnManager)}] ✓ PlayerOne using Keyboard + Mouse");
                }
                else
                {
                    Debug.LogWarning($"[{nameof(SpawnManager)}] No keyboard/mouse found!");
                }
            }
        }

        // ═══════════════════════════════════════════════
        //  GAMEPLAY ENABLE AFTER RESPAWN
        // ═══════════════════════════════════════════════
        
        private void EnableGameplayForRespawnedPlayer(string playerTag)
        {
            GameObject player = GameObject.Find(playerTag);
            if (player == null) return;
            
            EnableCarShooter(player);
            EnablePlayerUI(player);
        }
        
        private void EnableCarShooter(GameObject player)
        {
            CarShooter shooter = player.GetComponent<CarShooter>();
            if (shooter != null)
                shooter.EnableGameplay();
        }
        
        private void EnablePlayerUI(GameObject player)
        {
            PlayerUIManager uiManager = player.GetComponent<PlayerUIManager>();
            if (uiManager != null)
                uiManager.EnableGameplay();
        }

        // ═══════════════════════════════════════════════
        //  INPUT HELPERS
        // ═══════════════════════════════════════════════
        
        private void RestorePlayerInput(GameObject player, PlayerInput prefabInput, bool wasEnabled)
        {
            PlayerInput instanceInput = player.GetComponent<PlayerInput>();
            if (instanceInput != null)
                instanceInput.enabled = true;
            
            prefabInput.enabled = wasEnabled;
        }
    }
}