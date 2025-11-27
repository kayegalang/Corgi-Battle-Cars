using System.Collections.Generic;
    using UnityEngine;
    using Unity.Cinemachine;
    using UnityEngine.InputSystem;

    namespace _Player.Scripts
    {
        public class PlayerManager : MonoBehaviour
        {
            private List<PlayerInput> players = new List<PlayerInput>();
            //[SerializeField]
            //private List<Transform> startingPoints;
            [SerializeField]
            private List<LayerMask> playerLayers;

            private PlayerInputManager playerInputManager;

            private void Awake()
            {
                playerInputManager = FindFirstObjectByType<PlayerInputManager>();
            }

            private void OnEnable()
            {
                playerInputManager.onPlayerJoined += AddPlayer;
            }

            private void OnDisable()
            {
                playerInputManager.onPlayerJoined -= AddPlayer;
            }

            public void AddPlayer(PlayerInput player)
            {
                Debug.Log($"[PlayerManager] AddPlayer called for: {player.gameObject.name}, tag: '{player.gameObject.tag}'");
                
                // Check if this exact PlayerInput instance is already registered
                if (players.Contains(player))
                {
                    Debug.Log($"[PlayerManager] This PlayerInput instance is already registered, skipping");
                    return;
                }
                
                // Check if this player already has a tag (respawn case)
                int existingPlayerIndex = GetPlayerIndexFromTag(player.gameObject.tag);
                
                if (existingPlayerIndex >= 0)
                {
                    // This is a respawned player - use their existing index/layer
                    Debug.Log($"[PlayerManager] Detected player with existing tag '{player.gameObject.tag}', using index {existingPlayerIndex}");
                    
                    // Update the list to point to the new instance (old one was destroyed)
                    if (existingPlayerIndex < players.Count)
                    {
                        players[existingPlayerIndex] = player;
                    }
                    
                    AssignPlayerLayer(player, existingPlayerIndex);
                    return;
                }
                
                // This is a new player joining
                players.Add(player);
                
                int playerIndex = players.Count - 1;
                
                Debug.Log($"[PlayerManager] New player, assigned index {playerIndex} (total players: {players.Count})");

                // Check if we have enough player layers
                if (playerIndex >= playerLayers.Count)
                {
                    Debug.LogError($"Not enough player layers! Player {players.Count} cannot be assigned a layer. Please add more layers to the PlayerManager.");
                    return;
                }

                // Assign tag based on player index
                string playerTag = GetPlayerTag(playerIndex + 1);
                player.gameObject.tag = playerTag;
                player.gameObject.name = playerTag;
                
                Debug.Log($"[PlayerManager] New player joined: {playerTag}");
                
                // Assign layer
                AssignPlayerLayer(player, playerIndex);
            }
            
            private void AssignPlayerLayer(PlayerInput player, int playerIndex)
            {
                // PlayerInput is now directly on the parent (DefaultPlayer)
                Transform playerParent = player.transform;

                // Convert layer mask (bit) to an integer 
                int layerToAdd = (int)Mathf.Log(playerLayers[playerIndex].value, 2);

                // Find Camera and CinemachineCamera as direct children
                CinemachineCamera cinemachineCamera = playerParent.GetComponentInChildren<CinemachineCamera>();
                Camera playerCamera = playerParent.GetComponentInChildren<Camera>();
                CinemachineBrain cinemachineBrain = playerParent.GetComponentInChildren<CinemachineBrain>();

                // Set the layer for CinemachineCamera so the Brain can find it
                if (cinemachineCamera != null)
                {
                    cinemachineCamera.gameObject.layer = layerToAdd;
                }

                // Configure the Camera
                if (playerCamera != null)
                {
                    // Set the Camera GameObject's layer
                    playerCamera.gameObject.layer = layerToAdd;
                    
                    // Start with rendering everything
                    int cullingMask = ~0;
                    
                    // Remove all other player layers from the culling mask
                    for (int i = 0; i < playerLayers.Count; i++)
                    {
                        if (i != playerIndex) // Don't remove our own layer
                        {
                            int otherLayer = (int)Mathf.Log(playerLayers[i].value, 2);
                            cullingMask &= ~(1 << otherLayer); // Remove this layer from culling mask
                        }
                    }
                    
                    playerCamera.cullingMask = cullingMask;
                }

                // Configure the CinemachineBrain to only listen to cameras on this layer
                if (cinemachineBrain != null)
                {
                    cinemachineBrain.ChannelMask = (OutputChannels)(1 << layerToAdd);
                }
            }
            
            // Get player index from tag (PlayerOne = 0, PlayerTwo = 1, etc.)
            private int GetPlayerIndexFromTag(string tag)
            {
                return tag switch
                {
                    "PlayerOne" => 0,
                    "PlayerTwo" => 1,
                    "PlayerThree" => 2,
                    "PlayerFour" => 3,
                    _ => -1 // Not a player tag
                };
            }
            
            // Get player tag from index
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
        }
    }