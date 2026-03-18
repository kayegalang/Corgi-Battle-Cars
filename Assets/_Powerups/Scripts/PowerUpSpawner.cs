using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace _PowerUps.Scripts
{
    /// <summary>
    /// Spawns power-ups randomly within map boundaries!
    /// Just set 4 corner points - spawner picks random X/Z and raycasts to ground!
    /// SUPER EASY SETUP! No zones needed! ✨
    /// </summary>
    public class PowerUpSpawner : MonoBehaviour
    {
        [Header("Map Boundaries (4 Corners)")]
        [Tooltip("Place 4 empty GameObjects at map corners (any order)")]
        [SerializeField] private Transform[] boundaryCorners = new Transform[4];
        
        [Header("Power-Up Prefabs")]
        [Tooltip("All power-up pickup prefabs")]
        [SerializeField] private GameObject[] powerUpPrefabs;
        
        [Header("Spawn Settings")]
        [Tooltip("Minimum time between spawns (seconds)")]
        [SerializeField] public float minSpawnInterval = 5f;
        
        [Tooltip("Maximum time between spawns (seconds)")]
        [SerializeField] public float maxSpawnInterval = 10f;
        
        [Tooltip("Maximum power-ups that can exist at once")]
        [SerializeField] public int maxActivePowerUps = 10;
        
        [Tooltip("Spawn power-ups slightly above ground (prevents clipping)")]
        [SerializeField] public float verticalOffset = 0.5f;
        
        [Header("Ground Detection")]
        [Tooltip("Start raycast this high above the map")]
        [SerializeField] private float raycastStartHeight = 100f;
        
        [Tooltip("Maximum raycast distance down")]
        [SerializeField] private float raycastDistance = 200f;
        
        [Tooltip("Layers that count as 'ground' (ground, platforms, ramps)")]
        [SerializeField] private LayerMask groundLayers = ~0; // Default: everything
        
        [Tooltip("Don't spawn higher than this (avoids rooftops/tall platforms)")]
        [SerializeField] private float maxSpawnHeight = 20f;
        
        [Header("Collision Detection")]
        [Tooltip("Check for obstacles before spawning? (Avoids poles, trees, walls)")]
        [SerializeField] private bool enableCollisionChecking = true;
        
        [Tooltip("Layers to check for obstacles (e.g., walls, poles, trees)")]
        [SerializeField] private LayerMask obstacleLayers = ~0;
        
        [Tooltip("Radius around spawn point to check for obstacles")]
        [SerializeField] private float collisionCheckRadius = 0.5f;
        
        [Tooltip("How many times to try finding a clear spot before giving up")]
        [SerializeField] private int maxSpawnAttempts = 30;
        
        [Header("Auto-Start")]
        [Tooltip("Start spawning automatically when scene loads")]
        [SerializeField] private bool autoStart = false;
        
        private List<GameObject> activePowerUps = new List<GameObject>();
        private bool isSpawning = false;
        private Coroutine spawnCoroutine;
        
        // Calculated boundaries
        private float minX, maxX, minZ, maxZ;
        private bool boundariesCalculated = false;
        
        private void Start()
        {
            ValidateSetup();
            CalculateBoundaries();
            
            if (autoStart)
            {
                StartSpawning();
            }
        }
        
        private void ValidateSetup()
        {
            if (boundaryCorners == null || boundaryCorners.Length != 4)
            {
                Debug.LogError($"[{nameof(PowerUpSpawner)}] Need exactly 4 boundary corners! Create 4 empty GameObjects at map corners.");
                return;
            }
            
            for (int i = 0; i < boundaryCorners.Length; i++)
            {
                if (boundaryCorners[i] == null)
                {
                    Debug.LogError($"[{nameof(PowerUpSpawner)}] Boundary Corner {i} is null! Assign all 4 corners.");
                }
            }
            
            if (powerUpPrefabs == null || powerUpPrefabs.Length == 0)
            {
                Debug.LogError($"[{nameof(PowerUpSpawner)}] No power-up prefabs assigned!");
            }
            
            Debug.Log($"[{nameof(PowerUpSpawner)}] Boundary-based spawner ready! Just 4 corners needed! ✨");
        }
        
        private void CalculateBoundaries()
        {
            if (boundaryCorners == null || boundaryCorners.Length != 4)
            {
                return;
            }
            
            // Find min/max X and Z from the 4 corners
            minX = float.MaxValue;
            maxX = float.MinValue;
            minZ = float.MaxValue;
            maxZ = float.MinValue;
            
            foreach (Transform corner in boundaryCorners)
            {
                if (corner == null) continue;
                
                Vector3 pos = corner.position;
                
                if (pos.x < minX) minX = pos.x;
                if (pos.x > maxX) maxX = pos.x;
                if (pos.z < minZ) minZ = pos.z;
                if (pos.z > maxZ) maxZ = pos.z;
            }
            
            boundariesCalculated = true;
            
            Debug.Log($"[{nameof(PowerUpSpawner)}] Boundaries calculated: X({minX:F1} to {maxX:F1}), Z({minZ:F1} to {maxZ:F1})");
        }
        
        /// <summary>
        /// Start spawning power-ups! Call this when the match begins.
        /// </summary>
        public void StartSpawning()
        {
            if (isSpawning)
            {
                Debug.LogWarning($"[{nameof(PowerUpSpawner)}] Already spawning!");
                return;
            }
            
            if (!boundariesCalculated)
            {
                CalculateBoundaries();
            }
            
            isSpawning = true;
            
            // Spawn initial batch immediately
            SpawnInitialPowerUps();
            
            // Start continuous spawning
            spawnCoroutine = StartCoroutine(SpawnLoop());
            
            Debug.Log($"[{nameof(PowerUpSpawner)}] Started spawning power-ups! 🎁");
        }
        
        /// <summary>
        /// Stop spawning power-ups. Call this when the match ends.
        /// </summary>
        public void StopSpawning()
        {
            if (!isSpawning)
            {
                return;
            }
            
            isSpawning = false;
            
            if (spawnCoroutine != null)
            {
                StopCoroutine(spawnCoroutine);
                spawnCoroutine = null;
            }
            
            Debug.Log($"[{nameof(PowerUpSpawner)}] Stopped spawning power-ups");
        }
        
        /// <summary>
        /// Clear all active power-ups from the map
        /// </summary>
        public void ClearAllPowerUps()
        {
            foreach (GameObject powerUp in activePowerUps)
            {
                if (powerUp != null)
                {
                    Destroy(powerUp);
                }
            }
            
            activePowerUps.Clear();
            
            Debug.Log($"[{nameof(PowerUpSpawner)}] Cleared all power-ups");
        }
        
        private void SpawnInitialPowerUps()
        {
            // Spawn 2-3 power-ups immediately at the start
            int initialCount = Random.Range(2, 4);
            initialCount = Mathf.Min(initialCount, maxActivePowerUps);
            
            for (int i = 0; i < initialCount; i++)
            {
                SpawnRandomPowerUp();
            }
        }
        
        private IEnumerator SpawnLoop()
        {
            while (isSpawning)
            {
                // Wait random time before next spawn
                float waitTime = Random.Range(minSpawnInterval, maxSpawnInterval);
                yield return new WaitForSeconds(waitTime);
                
                // Clean up destroyed power-ups from list
                CleanUpDestroyedPowerUps();
                
                // Spawn if we haven't hit the limit
                if (activePowerUps.Count < maxActivePowerUps)
                {
                    SpawnRandomPowerUp();
                }
            }
        }
        
        private void SpawnRandomPowerUp()
        {
            if (!boundariesCalculated || powerUpPrefabs.Length == 0)
            {
                Debug.LogWarning($"[{nameof(PowerUpSpawner)}] Cannot spawn - boundaries not set or no prefabs!");
                return;
            }
            
            // Pick random power-up prefab
            GameObject randomPrefab = powerUpPrefabs[Random.Range(0, powerUpPrefabs.Length)];
            
            // Try to find a valid spawn position
            Vector3? spawnPosition = FindValidSpawnPosition();
            
            if (!spawnPosition.HasValue)
            {
                Debug.LogWarning($"[{nameof(PowerUpSpawner)}] Could not find valid spawn position after {maxSpawnAttempts} attempts!");
                return;
            }
            
            // Spawn the power-up!
            GameObject powerUp = Instantiate(randomPrefab, spawnPosition.Value, Quaternion.identity);
            
            // Track it
            activePowerUps.Add(powerUp);
            
            Debug.Log($"[{nameof(PowerUpSpawner)}] Spawned {powerUp.name} at {spawnPosition.Value} (Y={spawnPosition.Value.y:F1})! Active: {activePowerUps.Count}/{maxActivePowerUps}");
        }
        
        /// <summary>
        /// Finds a valid spawn position by:
        /// 1. Picking random X/Z within boundaries
        /// 2. Raycasting down to find ground
        /// 3. Checking height limit
        /// 4. Checking for obstacles (if enabled)
        /// </summary>
        private Vector3? FindValidSpawnPosition()
        {
            for (int attempt = 0; attempt < maxSpawnAttempts; attempt++)
            {
                // 1. Pick random X/Z within boundaries
                float randomX = Random.Range(minX, maxX);
                float randomZ = Random.Range(minZ, maxZ);
                
                // Start raycast high above the map
                Vector3 raycastStart = new Vector3(randomX, raycastStartHeight, randomZ);
                
                // 2. Raycast down to find ground
                if (!Physics.Raycast(raycastStart, Vector3.down, out RaycastHit hit, raycastDistance, groundLayers))
                {
                    // No ground found here, try again
                    Debug.DrawRay(raycastStart, Vector3.down * raycastDistance, Color.red, 1f);
                    continue;
                }
                
                // Found ground!
                Vector3 groundPosition = hit.point;
                
                // 3. Check height limit (don't spawn on rooftops!)
                if (groundPosition.y > maxSpawnHeight)
                {
                    Debug.DrawLine(raycastStart, groundPosition, Color.yellow, 1f);
                    continue;
                }
                
                // 4. Add vertical offset to prevent clipping
                Vector3 spawnPosition = groundPosition + Vector3.up * verticalOffset;
                
                // 5. Check for obstacles (if enabled)
                if (enableCollisionChecking)
                {
                    if (!IsPositionClear(spawnPosition))
                    {
                        // Blocked by obstacle, try again
                        continue;
                    }
                }
                
                // Found a valid position! ✅
                Debug.DrawLine(raycastStart, spawnPosition, Color.green, 2f);
                return spawnPosition;
            }
            
            // Failed to find position after max attempts
            return null;
        }
        
        /// <summary>
        /// Checks if a position is clear of obstacles
        /// </summary>
        private bool IsPositionClear(Vector3 position)
        {
            // Check for obstacles in a sphere around the position
            Collider[] overlaps = Physics.OverlapSphere(position, collisionCheckRadius, obstacleLayers);
            
            // Filter out trigger colliders
            foreach (Collider overlap in overlaps)
            {
                // Ignore triggers
                if (overlap.isTrigger)
                {
                    continue;
                }
                
                // Found a solid obstacle! Position is blocked! ❌
                Debug.DrawLine(position, overlap.transform.position, Color.red, 1f);
                return false;
            }
            
            // No solid obstacles found! Position is clear! ✅
            return true;
        }
        
        private void CleanUpDestroyedPowerUps()
        {
            // Remove null references (picked up power-ups)
            activePowerUps.RemoveAll(p => p == null);
        }
        
        private void OnDestroy()
        {
            StopSpawning();
        }
        
        // ═══════════════════════════════════════════════
        //  EDITOR VISUALIZATION
        // ═══════════════════════════════════════════════
        
        private void OnDrawGizmos()
        {
            if (boundaryCorners == null || boundaryCorners.Length != 4) return;
            
            // Check if all corners are assigned
            bool allCornersValid = true;
            foreach (Transform corner in boundaryCorners)
            {
                if (corner == null)
                {
                    allCornersValid = false;
                    break;
                }
            }
            
            if (!allCornersValid) return;
            
            // Calculate boundaries for gizmo
            float gizmoMinX = float.MaxValue;
            float gizmoMaxX = float.MinValue;
            float gizmoMinZ = float.MaxValue;
            float gizmoMaxZ = float.MinValue;
            
            foreach (Transform corner in boundaryCorners)
            {
                Vector3 pos = corner.position;
                if (pos.x < gizmoMinX) gizmoMinX = pos.x;
                if (pos.x > gizmoMaxX) gizmoMaxX = pos.x;
                if (pos.z < gizmoMinZ) gizmoMinZ = pos.z;
                if (pos.z > gizmoMaxZ) gizmoMaxZ = pos.z;
            }
            
            // Draw boundary rectangle
            Gizmos.color = new Color(0f, 1f, 0f, 0.3f); // Green transparent
            
            Vector3 corner1 = new Vector3(gizmoMinX, 0, gizmoMinZ);
            Vector3 corner2 = new Vector3(gizmoMaxX, 0, gizmoMinZ);
            Vector3 corner3 = new Vector3(gizmoMaxX, 0, gizmoMaxZ);
            Vector3 corner4 = new Vector3(gizmoMinX, 0, gizmoMaxZ);
            
            // Draw lines connecting corners
            Gizmos.DrawLine(corner1, corner2);
            Gizmos.DrawLine(corner2, corner3);
            Gizmos.DrawLine(corner3, corner4);
            Gizmos.DrawLine(corner4, corner1);
            
            // Draw corner markers
            Gizmos.color = Color.green;
            foreach (Transform corner in boundaryCorners)
            {
                Gizmos.DrawSphere(corner.position, 0.5f);
            }
        }
        
        private void OnDrawGizmosSelected()
        {
            if (boundaryCorners == null || boundaryCorners.Length != 4) return;
            
            // Draw more detailed boundary when selected
            OnDrawGizmos();
            
            // Draw max height plane
            if (boundariesCalculated || Application.isPlaying)
            {
                Gizmos.color = new Color(1f, 0f, 0f, 0.1f); // Red transparent
                
                Vector3 center = new Vector3((minX + maxX) / 2f, maxSpawnHeight, (minZ + maxZ) / 2f);
                Vector3 size = new Vector3(maxX - minX, 0.1f, maxZ - minZ);
                
                Gizmos.DrawCube(center, size);
            }
        }
    }
}