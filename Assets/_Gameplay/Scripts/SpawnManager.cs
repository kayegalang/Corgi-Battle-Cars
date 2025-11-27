using System.Collections;
using System.Collections.Generic;
using _Cars.Scripts;
using UnityEngine;

namespace _Gameplay.Scripts 
{
    public class SpawnManager : MonoBehaviour
    {
        [SerializeField] private Transform[] spawnPoints;
        [SerializeField] private GameObject playerPrefab;
        [SerializeField] private GameObject botPrefab;
        
        private readonly HashSet<int> usedSpawnIndices = new HashSet<int>();

        void Awake()
        {
            usedSpawnIndices.Clear();
        }
        
        private Transform GetSpawnPoint()
        {
            return spawnPoints[Random.Range(0, spawnPoints.Length)];
        }

        private Transform GetUniqueSpawnPoint()
        {
            if (usedSpawnIndices.Count >= spawnPoints.Length)
            {
                usedSpawnIndices.Clear();
            }

            int randomIndex;
            
            do
            {
                randomIndex = Random.Range(0, spawnPoints.Length);
            } 
            while (usedSpawnIndices.Contains(randomIndex));

            usedSpawnIndices.Add(randomIndex);

            return spawnPoints[randomIndex];
        }

        public void StartSingleplayerGame()
        {
            Spawn("PlayerOne", GetUniqueSpawnPoint(), playerPrefab);
            Spawn("BotOne",  GetUniqueSpawnPoint(), botPrefab);
            Spawn("BotTwo", GetUniqueSpawnPoint(), botPrefab);
            Spawn("BotThree", GetUniqueSpawnPoint(), botPrefab);
            
            GameplayManager.instance.UpdatePlayerList("PlayerOne");
            GameplayManager.instance.UpdatePlayerList("BotOne");
            GameplayManager.instance.UpdatePlayerList("BotTwo");
            GameplayManager.instance.UpdatePlayerList("BotThree");
        }
        
        public void StartMultiplayerGame(int humanPlayerCount)
        {
            Debug.Log($"[SpawnManager] Starting multiplayer game with {humanPlayerCount} human players");
            
            // Spawn human players using playerPrefab (with PlayerInput)
            for (int i = 1; i <= humanPlayerCount; i++)
            {
                string playerTag = GetPlayerTag(i);
                Spawn(playerTag, GetUniqueSpawnPoint(), playerPrefab);
                GameplayManager.instance.UpdatePlayerList(playerTag);
                Debug.Log($"[SpawnManager] Spawned human player: {playerTag}");
            }
            
            // Calculate how many bots we need (total should be 4)
            int botsNeeded = 4 - humanPlayerCount;
            
            // Spawn bots to fill remaining slots
            for (int i = 0; i < botsNeeded; i++)
            {
                int botNumber = humanPlayerCount + i + 1;
                string botTag = GetBotTag(botNumber);
                
                SpawnBot(botTag, GetUniqueSpawnPoint());
                GameplayManager.instance.UpdatePlayerList(botTag);
                
                Debug.Log($"[SpawnManager] Spawned bot: {botTag}");
            }
        }
        
        // Helper to get player tag (PlayerOne, PlayerTwo, etc.)
        private string GetPlayerTag(int playerNumber)
        {
            return playerNumber switch
            {
                1 => "PlayerOne",
                2 => "PlayerTwo",
                3 => "PlayerThree",
                4 => "PlayerFour",
                _ => "PlayerOne"
            };
        }
        
        // Helper to get bot tag based on position (BotOne, BotTwo, etc.)
        private string GetBotTag(int botPosition)
        {
            return botPosition switch
            {
                1 => "BotOne",
                2 => "BotTwo",
                3 => "BotThree",
                4 => "BotFour",
                _ => "BotOne"
            };
        }

        public void Respawn(string playerTag)
        {
            GameObject prefabToSpawn = null;
            
            if (playerTag.StartsWith("Bot"))
            {
                prefabToSpawn = botPrefab;
            }
            else if (playerTag.StartsWith("Player"))
            {
                prefabToSpawn = playerPrefab;
            }
            
            StartCoroutine(WaitToRespawn(playerTag, prefabToSpawn, GetSpawnPoint()));
        }

        private void Spawn(string playerTag, Transform spawnPoint, GameObject prefab)
        {
            Debug.Log($"[SpawnManager] Spawning {playerTag}");
            
            // Check if this prefab has PlayerInput
            UnityEngine.InputSystem.PlayerInput prefabInput = prefab.GetComponent<UnityEngine.InputSystem.PlayerInput>();
            bool hasPlayerInput = prefabInput != null;
            bool wasEnabled = false;
            
            // If it has PlayerInput, disable it temporarily
            if (hasPlayerInput)
            {
                wasEnabled = prefabInput.enabled;
                prefabInput.enabled = false;
            }
            
            // Instantiate
            GameObject player = Instantiate(prefab, spawnPoint.position, spawnPoint.rotation);
            
            // Set tag BEFORE enabling PlayerInput
            player.tag = playerTag;
            player.name = playerTag;
            
            // Re-enable PlayerInput if it exists
            if (hasPlayerInput)
            {
                UnityEngine.InputSystem.PlayerInput instanceInput = player.GetComponent<UnityEngine.InputSystem.PlayerInput>();
                if (instanceInput != null)
                {
                    instanceInput.enabled = true;
                }
                
                // Restore prefab state
                prefabInput.enabled = wasEnabled;
            }
            
            Debug.Log($"[SpawnManager] Spawned {playerTag} at {spawnPoint.position}");
        }
        
        private void SpawnBot(string botTag, Transform spawnPoint)
        {
            // Spawn bot (no PlayerInput, so no complications)
            GameObject bot = Instantiate(botPrefab, spawnPoint.position, spawnPoint.rotation);
            bot.tag = botTag;
            bot.name = botTag;
            
            Debug.Log($"[SpawnManager] Spawned bot {botTag} at {spawnPoint.position}");
        }

        private IEnumerator WaitToRespawn(string playerTag, GameObject playerObject, Transform spawnPoint)
        { 
            yield return new WaitForSeconds(3f);
            
            // CRITICAL: Disable PlayerInput on prefab before instantiating
            UnityEngine.InputSystem.PlayerInput prefabInput = playerObject.GetComponent<UnityEngine.InputSystem.PlayerInput>();
            bool wasEnabled = false;
            if (prefabInput != null)
            {
                wasEnabled = prefabInput.enabled;
                prefabInput.enabled = false;
            }
            
            GameObject player = Instantiate(playerObject, spawnPoint.position, spawnPoint.rotation);
            
            // Set tag BEFORE enabling PlayerInput
            player.tag = playerTag;
            player.name = playerTag;
            
            Debug.Log($"[SpawnManager] Respawned {playerTag}, tag set, now enabling PlayerInput");
            
            // Now enable PlayerInput - this will trigger onPlayerJoined with correct tag
            UnityEngine.InputSystem.PlayerInput instanceInput = player.GetComponent<UnityEngine.InputSystem.PlayerInput>();
            if (instanceInput != null)
            {
                instanceInput.enabled = true;
            }
            
            // Restore prefab state
            if (prefabInput != null)
            {
                prefabInput.enabled = wasEnabled;
            }
            
            // Enable gameplay for this respawned player
            CarShooter shooter = player.GetComponent<CarShooter>();
            if (shooter != null)
            {
                shooter.EnableGameplay();
                Debug.Log($"[SpawnManager] Enabled gameplay for respawned {playerTag}");
            }
            
            Debug.Log($"[SpawnManager] Respawn complete for {playerTag}");
        }
    }
}