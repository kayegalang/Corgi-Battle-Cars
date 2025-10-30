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
            StartCoroutine(WaitToSpawn(playerTag, prefabToSpawn, GetSpawnPoint()));
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
            StartCoroutine(WaitToSpawn(playerTag, prefabToSpawn, spawnPoint));
        }

        private IEnumerator WaitToSpawn(string playerTag, GameObject playerObject, Transform spawnPoint)
        { 
            yield return new WaitForSeconds(3f);
            
            GameObject player = Instantiate(playerObject, spawnPoint.position, spawnPoint.rotation);
            player.tag = playerTag;
        }
        
    }
}

