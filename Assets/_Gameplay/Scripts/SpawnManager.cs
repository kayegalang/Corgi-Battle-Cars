using System.Collections;
using System.Collections.Generic;
using _Cars.Scripts;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _Gameplay.Scripts
{
    public class SpawnManager : MonoBehaviour
    {
        [SerializeField] private Transform[] spawnPoints;
        [SerializeField] private GameObject playerPrefab; // Fallback only
        [SerializeField] private GameObject botPrefab;

        private readonly HashSet<int> usedSpawnIndices = new();

        private void Awake()
        {
            usedSpawnIndices.Clear();
        }

        private Transform GetUniqueSpawnPoint()
        {
            if (usedSpawnIndices.Count >= spawnPoints.Length)
                usedSpawnIndices.Clear();

            int randomIndex;
            do
            {
                randomIndex = Random.Range(0, spawnPoints.Length);
            }
            while (usedSpawnIndices.Contains(randomIndex));

            usedSpawnIndices.Add(randomIndex);
            return spawnPoints[randomIndex];
        }

        // ============================================================
        // SINGLEPLAYER - Now uses PlayerInputManager
        // ============================================================

        public void StartSingleplayerGame()
        {
            usedSpawnIndices.Clear();

            // Find the player that was spawned via PlayerInputManager
            var playerObj = GameObject.FindGameObjectWithTag("Player1");
            
            if (playerObj != null)
            {
                // Position the existing player
                Transform sp = GetUniqueSpawnPoint();
                
                // Make rigidbody kinematic temporarily
                var rb = playerObj.GetComponentInChildren<Rigidbody>();
                if (rb != null)
                {
                    rb.isKinematic = true;
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }
                
                playerObj.transform.position = sp.position;
                playerObj.transform.rotation = sp.rotation;
                
                // RE-ENABLE PLAYER FOR SINGLEPLAYER
                EnableSingleplayer(playerObj);
                
                // Re-enable physics
                if (rb != null)
                {
                    rb.isKinematic = false;
                }
                
                GameplayManager.instance.UpdatePlayerList("Player1");
                Debug.Log("Positioned existing Player1 for singleplayer");
            }
            else
            {
                // Fallback: spawn new player if none exists
                Debug.LogWarning("No Player1 found, spawning fallback player prefab");
                SpawnNew(playerPrefab, "Player1");
            }

            // Spawn bots
            SpawnNew(botPrefab, "Bot1");
            SpawnNew(botPrefab, "Bot2");
            SpawnNew(botPrefab, "Bot3");
        }

        private void EnableSingleplayer(GameObject playerObj)
        {
            Debug.Log($"Enabling singleplayer gameplay for {playerObj.name}");

            // Re-enable cameras
            var cameras = playerObj.GetComponentsInChildren<Camera>(true);
            foreach (var cam in cameras)
            {
                cam.enabled = true;
                Debug.Log($"Enabled camera: {cam.name}");
            }

            // Re-enable Canvas
            var canvases = playerObj.GetComponentsInChildren<Canvas>(true);
            foreach (var canvas in canvases)
            {
                canvas.enabled = true;
                Debug.Log($"Enabled canvas: {canvas.name}");
            }

            // Re-enable INPUT ACTIONS
            var input = playerObj.GetComponentInChildren<UnityEngine.InputSystem.PlayerInput>(true);
            if (input != null && input.actions != null)
            {
                input.ActivateInput();
                Debug.Log("Activated input for singleplayer");
            }

            // Re-enable CarController
            var controllers = playerObj.GetComponentsInChildren<MonoBehaviour>(true);
            foreach (var controller in controllers)
            {
                if (controller.GetType().Name == "CarController")
                {
                    controller.enabled = true;
                    Debug.Log("Enabled CarController for singleplayer");
                }
            }
        }

        // ============================================================
        // MULTIPLAYER
        // ============================================================

        public void StartMultiplayerGame(_Player.Scripts.PlayerManager playerManager)
        {
            usedSpawnIndices.Clear();

            var mp = MultiplayerManager.instance;
            if (mp == null)
            {
                Debug.LogError("MultiplayerManager instance is null!");
                return;
            }

            var joinedRoots = mp.GetJoinedPlayerRoots();
            int realPlayers = joinedRoots.Count;

            Debug.Log($"=== Starting multiplayer game with {realPlayers} players ===");

            int playerIndex = 1;

            foreach (var root in joinedRoots)
            {
                if (root == null)
                {
                    Debug.LogWarning($"Player root at index {playerIndex} is null, skipping");
                    continue;
                }

                Debug.Log($"Processing player {playerIndex}: {root.name}");

                // Move into this map scene
                SceneManager.MoveGameObjectToScene(root, SceneManager.GetActiveScene());

                // Get spawn point
                Transform sp = GetUniqueSpawnPoint();
                Debug.Log($"Spawn point for player {playerIndex}: {sp.name} at position {sp.position}");

                // Find all rigidbodies and make them kinematic
                var allRbs = root.GetComponentsInChildren<Rigidbody>(true);
                Debug.Log($"Found {allRbs.Length} rigidbodies on player {playerIndex}");
                
                foreach (var rb in allRbs)
                {
                    rb.isKinematic = true;
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }

                // Set root position
                root.transform.position = sp.position;
                root.transform.rotation = sp.rotation;

                Debug.Log($"Player {playerIndex} root position set to: {root.transform.position}");

                // Tag + track
                string tag = "Player" + playerIndex;
                root.tag = tag;
                GameplayManager.instance.UpdatePlayerList(tag);

                // Rewire split-screen cameras
                var input = root.GetComponentInChildren<UnityEngine.InputSystem.PlayerInput>();
                if (playerManager != null && input != null)
                    playerManager.SetupPlayer(input, playerIndex);

                // RE-ENABLE GAMEPLAY COMPONENTS
                Debug.Log($"About to enable gameplay for player {playerIndex}");
                mp.EnablePlayerGameplay(root);

                // Verify final position after re-enable
                Debug.Log($"Player {playerIndex} FINAL position after enable: {root.transform.position}");

                playerIndex++;
            }

            Debug.Log($"=== Finished setting up {playerIndex - 1} players ===");

            // Fill remaining slots with bots up to 4 total
            int botsToSpawn = 4 - realPlayers;
            for (int i = 1; i <= botsToSpawn; i++)
            {
                SpawnNew(botPrefab, "Bot" + i);
            }
        }

        private void SpawnNew(GameObject prefab, string tag)
        {
            Transform sp = GetUniqueSpawnPoint();
            GameObject obj = Instantiate(prefab, sp.position, sp.rotation);
            obj.tag = tag;

            GameplayManager.instance.UpdatePlayerList(tag);
        }

        // ============================================================
        // RESPAWN - Unified for both modes
        // ============================================================

        public void Respawn(string playerTag)
        {
            if (playerTag.StartsWith("Player"))
            {
                // Players use the reposition method
                StartCoroutine(RespawnPlayer(playerTag));
            }
            else
            {
                // Bots get destroyed and respawned
                StartCoroutine(WaitToSpawn(playerTag, botPrefab));
            }
        }

        private IEnumerator RespawnPlayer(string playerTag)
        {
            yield return new WaitForSeconds(3f);

            GameObject playerObj = GameObject.FindGameObjectWithTag(playerTag);
            
            if (playerObj != null)
            {
                // Get a new spawn point
                Transform sp = GetUniqueSpawnPoint();
                
                // Add a small upward offset to ensure we spawn ABOVE the ground
                Vector3 spawnPosition = sp.position + Vector3.up * 1f; // 1 unit above spawn point
                
                Debug.Log($"[RESPAWN] {playerTag} - Target spawn: {sp.name} at {sp.position}");
                Debug.Log($"[RESPAWN] {playerTag} - Adjusted spawn position (with offset): {spawnPosition}");
                Debug.Log($"[RESPAWN] {playerTag} - Current position BEFORE anything: {playerObj.transform.position}");
                
                // Find the Player child GameObject
                Transform playerChild = playerObj.transform.Find("Player");
                if (playerChild == null)
                {
                    Debug.LogError($"Could not find 'Player' child on {playerTag}!");
                    yield break;
                }
                
                // Move to spawn point FIRST (while Player child is still inactive)
                playerObj.transform.position = spawnPosition;
                playerObj.transform.rotation = sp.rotation;
                Debug.Log($"[RESPAWN] {playerTag} - Set position BEFORE reactivate: {playerObj.transform.position}");
                
                // Find rigidbody (should be on the Player child, so need includeInactive=true)
                var rb = playerChild.GetComponentInChildren<Rigidbody>(true);
                if (rb != null)
                {
                    Debug.Log($"[RESPAWN] {playerTag} - Found Rigidbody, making kinematic");
                    rb.isKinematic = true;
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }
                else
                {
                    Debug.LogWarning($"[RESPAWN] {playerTag} - No Rigidbody found!");
                }
                
                // Reactivate the Player child
                playerChild.gameObject.SetActive(true);
                Debug.Log($"[RESPAWN] {playerTag} - Reactivated Player child");
                Debug.Log($"[RESPAWN] {playerTag} - Position AFTER reactivate: {playerObj.transform.position}");
                
                // Wait one frame for all components to initialize
                yield return null;
                Debug.Log($"[RESPAWN] {playerTag} - Position after 1 frame: {playerObj.transform.position}");
                
                // Force position again
                playerObj.transform.position = spawnPosition;
                playerObj.transform.rotation = sp.rotation;
                Debug.Log($"[RESPAWN] {playerTag} - Forced position after 1 frame: {playerObj.transform.position}");
                
                // Wait another frame
                yield return null;
                Debug.Log($"[RESPAWN] {playerTag} - Position after 2 frames: {playerObj.transform.position}");
                
                // Force position AGAIN
                playerObj.transform.position = spawnPosition;
                playerObj.transform.rotation = sp.rotation;
                Debug.Log($"[RESPAWN] {playerTag} - Forced position after 2 frames: {playerObj.transform.position}");
                
                // Reset health
                var health = playerChild.GetComponentInChildren<CarHealth>(true);
                if (health != null)
                {
                    health.ResetHealth();
                }
                
                // Finally, re-enable physics
                if (rb != null)
                {
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                    rb.isKinematic = false;
                    Debug.Log($"[RESPAWN] {playerTag} - Re-enabled physics");
                }
                
                Debug.Log($"[RESPAWN] ===== FINAL: {playerTag} at {playerObj.transform.position} (target was {spawnPosition}) =====");
            }
            else
            {
                Debug.LogError($"Could not find player {playerTag} to respawn!");
            }
        }

        private IEnumerator WaitToSpawn(string playerTag, GameObject prefab)
        {
            yield return new WaitForSeconds(3f);

            Transform sp = GetUniqueSpawnPoint();
            GameObject obj = Instantiate(prefab, sp.position, sp.rotation);
            obj.tag = playerTag;
            
            GameplayManager.instance.UpdatePlayerList(playerTag);
        }
    }
}