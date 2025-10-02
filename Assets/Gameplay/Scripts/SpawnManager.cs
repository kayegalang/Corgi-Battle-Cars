using System.Collections;
using UnityEngine;

namespace Gameplay.Scripts 
{
    public class SpawnManager : MonoBehaviour
    {
        [SerializeField] private GameObject playerPrefab;
        [SerializeField] private Transform[] spawnPoints;

        public Transform GetSpawnPoint()
        {
            return spawnPoints[Random.Range(0, spawnPoints.Length)];
        }

        public void Spawn(string playerTag)
        {
            StartCoroutine(WaitToSpawn(playerTag));
        }

        private IEnumerator WaitToSpawn(string playerTag)
        {
            yield return new WaitForSeconds(3f);
            
            Transform spawnPoint = GetSpawnPoint();
            
           GameObject player = Instantiate(playerPrefab, spawnPoint.position, spawnPoint.rotation);
           player.tag = playerTag;
        }
        
    }
}

