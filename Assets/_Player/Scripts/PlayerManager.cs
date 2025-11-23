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
                players.Add(player);
                
                int playerIndex = players.Count - 1;

                // Check if we have enough player layers
                if (playerIndex >= playerLayers.Count)
                {
                    Debug.LogError($"Not enough player layers! Player {players.Count} cannot be assigned a layer. Please add more layers to the PlayerManager.");
                    return;
                }

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
        }
    }