using System.Collections;
using System.Collections.Generic;
using _Cars.Scripts;
using _UI.Scripts;
using UnityEngine;

namespace _Gameplay.Scripts 
{
    public class SpawnManager : MonoBehaviour
    {
        [SerializeField] private Transform[] spawnPoints;
        [SerializeField] private GameObject playerPrefab;
        [SerializeField] private GameObject botPrefab;
        
        [Header("Spawn Settings")]
        [SerializeField] private float respawnDelay = 3f;
        
        private readonly HashSet<int> usedSpawnIndices = new HashSet<int>();
        
        private const int TOTAL_PLAYERS = 4;
        private const string PLAYER_TAG_PREFIX = "Player";
        private const string BOT_TAG_PREFIX = "Bot";
        
        private static readonly Dictionary<int, string> PlayerTagMap = new Dictionary<int, string>
        {
            { 1, "PlayerOne" },
            { 2, "PlayerTwo" },
            { 3, "PlayerThree" },
            { 4, "PlayerFour" }
        };
        
        private static readonly Dictionary<int, string> BotTagMap = new Dictionary<int, string>
        {
            { 1, "BotOne" },
            { 2, "BotTwo" },
            { 3, "BotThree" },
            { 4, "BotFour" }
        };
        
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
            {
                Debug.LogError($"[{nameof(SpawnManager)}] No spawn points assigned!");
            }
            
            if (playerPrefab == null)
            {
                Debug.LogError($"[{nameof(SpawnManager)}] Player prefab is not assigned!");
            }
            
            if (botPrefab == null)
            {
                Debug.LogError($"[{nameof(SpawnManager)}] Bot prefab is not assigned!");
            }
        }
        
        public void StartGame(int humanPlayerCount)
        {
            if (!ValidatePlayerCount(humanPlayerCount))
            {
                return;
            }
            
            SpawnHumanPlayers(humanPlayerCount);
            SpawnBots(humanPlayerCount);
        }
        
        private bool ValidatePlayerCount(int humanPlayerCount)
        {
            if (humanPlayerCount < 0 || humanPlayerCount > TOTAL_PLAYERS)
            {
                Debug.LogError($"[{nameof(SpawnManager)}] Invalid player count: {humanPlayerCount}. Must be between 0 and {TOTAL_PLAYERS}");
                return false;
            }
            
            return true;
        }
        
        private void SpawnHumanPlayers(int humanPlayerCount)
        {
            for (int i = 1; i <= humanPlayerCount; i++)
            {
                string playerTag = GetPlayerTag(i);
                SpawnPlayer(playerTag, GetUniqueSpawnPoint(), playerPrefab);
                RegisterPlayer(playerTag);
            }
        }
        
        private void SpawnBots(int humanPlayerCount)
        {
            int botsNeeded = TOTAL_PLAYERS - humanPlayerCount;
            
            for (int i = 0; i < botsNeeded; i++)
            {
                int botNumber = humanPlayerCount + i + 1;
                string botTag = GetBotTag(botNumber);
                
                SpawnBot(botTag, GetUniqueSpawnPoint());
                RegisterPlayer(botTag);
            }
        }
        
        private void RegisterPlayer(string playerTag)
        {
            if (GameplayManager.instance != null)
            {
                GameplayManager.instance.UpdatePlayerList(playerTag);
            }
        }
        
        private Transform GetRandomSpawnPoint()
        {
            if (spawnPoints.Length == 0)
            {
                Debug.LogError($"[{nameof(SpawnManager)}] No spawn points available!");
                return null;
            }
            
            return spawnPoints[Random.Range(0, spawnPoints.Length)];
        }
        
        private Transform GetUniqueSpawnPoint()
        {
            if (spawnPoints.Length == 0)
            {
                Debug.LogError($"[{nameof(SpawnManager)}] No spawn points available!");
                return null;
            }
            
            if (AllSpawnPointsUsed())
            {
                ClearUsedSpawnIndices();
            }
            
            int randomIndex = FindUnusedSpawnIndex();
            usedSpawnIndices.Add(randomIndex);
            
            return spawnPoints[randomIndex];
        }
        
        private bool AllSpawnPointsUsed()
        {
            return usedSpawnIndices.Count >= spawnPoints.Length;
        }
        
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
        
        private string GetPlayerTag(int playerNumber)
        {
            if (PlayerTagMap.TryGetValue(playerNumber, out string tag))
            {
                return tag;
            }
            
            Debug.LogWarning($"[{nameof(SpawnManager)}] Invalid player number: {playerNumber}, defaulting to PlayerOne");
            return "PlayerOne";
        }
        
        private string GetBotTag(int botPosition)
        {
            if (BotTagMap.TryGetValue(botPosition, out string tag))
            {
                return tag;
            }
            
            Debug.LogWarning($"[{nameof(SpawnManager)}] Invalid bot position: {botPosition}, defaulting to BotOne");
            return "BotOne";
        }
        
        public void Respawn(string playerTag)
        {
            GameObject prefabToSpawn = GetPrefabForTag(playerTag);
            
            if (prefabToSpawn == null)
            {
                Debug.LogError($"[{nameof(SpawnManager)}] Cannot respawn - no prefab found for tag: {playerTag}");
                return;
            }
            
            StartCoroutine(RespawnAfterDelay(playerTag, prefabToSpawn));
        }
        
        private GameObject GetPrefabForTag(string playerTag)
        {
            if (playerTag.StartsWith(BOT_TAG_PREFIX))
            {
                return botPrefab;
            }
            
            if (playerTag.StartsWith(PLAYER_TAG_PREFIX))
            {
                return playerPrefab;
            }
            
            return null;
        }
        
        private IEnumerator RespawnAfterDelay(string playerTag, GameObject prefab)
        {
            yield return new WaitForSeconds(respawnDelay);
            
            Transform spawnPoint = GetRandomSpawnPoint();
            
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
                SpawnPlayer(playerTag, spawnPoint, prefab);
            }
            
            EnableGameplayForRespawnedPlayer(playerTag);
        }
        
        private void SpawnPlayer(string playerTag, Transform spawnPoint, GameObject prefab)
        {
            UnityEngine.InputSystem.PlayerInput prefabInput = prefab.GetComponent<UnityEngine.InputSystem.PlayerInput>();
            bool hasPlayerInput = prefabInput != null;
            bool wasEnabled = false;
            
            if (hasPlayerInput)
            {
                wasEnabled = prefabInput.enabled;
                prefabInput.enabled = false;
            }
            
            GameObject player = Instantiate(prefab, spawnPoint.position, spawnPoint.rotation);
            
            SetPlayerIdentity(player, playerTag);
            
            if (hasPlayerInput)
            {
                RestorePlayerInput(player, prefabInput, wasEnabled);
            }
        }
        
        private void SpawnBot(string botTag, Transform spawnPoint)
        {
            GameObject bot = Instantiate(botPrefab, spawnPoint.position, spawnPoint.rotation);
            SetPlayerIdentity(bot, botTag);
        }
        
        private void SetPlayerIdentity(GameObject player, string playerTag)
        {
            player.tag = playerTag;
            player.name = playerTag;
        }
        
        private void RestorePlayerInput(GameObject player, UnityEngine.InputSystem.PlayerInput prefabInput, bool wasEnabled)
        {
            UnityEngine.InputSystem.PlayerInput instanceInput = player.GetComponent<UnityEngine.InputSystem.PlayerInput>();
            
            if (instanceInput != null)
            {
                instanceInput.enabled = true;
            }
            
            prefabInput.enabled = wasEnabled;
        }
        
        private void EnableGameplayForRespawnedPlayer(string playerTag)
        {
            GameObject player = GameObject.Find(playerTag);
            
            if (player == null)
            {
                return;
            }
            
            EnableCarShooter(player);
            EnablePlayerUI(player);
        }
        
        private void EnableCarShooter(GameObject player)
        {
            CarShooter shooter = player.GetComponent<CarShooter>();
            
            if (shooter != null)
            {
                shooter.EnableGameplay();
            }
        }
        
        private void EnablePlayerUI(GameObject player)
        {
            PlayerUIManager uiManager = player.GetComponent<PlayerUIManager>();
            
            if (uiManager != null)
            {
                uiManager.EnableGameplay();
            }
        }
    }
}