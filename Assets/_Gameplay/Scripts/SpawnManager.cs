using System.Collections;
using System.Collections.Generic;
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
            Spawn("PlayerOne", GetUniqueSpawnPoint());
            Spawn("BotOne",  GetUniqueSpawnPoint());
            Spawn("BotTwo", GetUniqueSpawnPoint());
            Spawn("BotThree", GetUniqueSpawnPoint());
            
            GameplayManager.instance.UpdatePlayerList("PlayerOne");
            GameplayManager.instance.UpdatePlayerList("BotOne");
            GameplayManager.instance.UpdatePlayerList("BotTwo");
            GameplayManager.instance.UpdatePlayerList("BotThree");
        }
        
        public void StartMultiplayerGame(int humanPlayerCount)
        {
            Debug.Log($"Starting multiplayer game with {humanPlayerCount} human players");
            
            // Spawn human players first
            for (int i = 1; i <= humanPlayerCount; i++)
            {
                string playerTag = GetPlayerTag(i);
                Spawn(playerTag, GetUniqueSpawnPoint());
                GameplayManager.instance.UpdatePlayerList(playerTag);
                Debug.Log($"Spawned human player: {playerTag}");
            }
            
            // Calculate how many bots we need (total should be 4)
            int botsNeeded = 4 - humanPlayerCount;
            
            // Spawn bots to fill remaining slots
            for (int i = 0; i < botsNeeded; i++)
            {
                int botNumber = humanPlayerCount + i + 1; // Bot numbers come after human players
                string botTag = GetBotTag(botNumber);
                
                Spawn(botTag, GetUniqueSpawnPoint());
                GameplayManager.instance.UpdatePlayerList(botTag);
                
                Debug.Log($"Spawning bot: {botTag}");
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

        private void Spawn(string playerTag, Transform spawnPoint)
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
            
            // Spawn immediately (no more 3 second wait!)
            GameObject player = Instantiate(prefabToSpawn, spawnPoint.position, spawnPoint.rotation);
            player.tag = playerTag;
            player.name = playerTag; // Also set the name for easier debugging
            
            Debug.Log($"Spawned {playerTag} at {spawnPoint.position}");
        }

        private IEnumerator WaitToRespawn(string playerTag, GameObject playerObject, Transform spawnPoint)
        { 
            yield return new WaitForSeconds(3f);
            
            GameObject player = Instantiate(playerObject, spawnPoint.position, spawnPoint.rotation);
            player.tag = playerTag;
            player.name = playerTag;
            
            Debug.Log($"Respawned {playerTag}");
        }
        
    }
}