using System.Collections.Generic;
using UnityEngine;
using Unity.Cinemachine;
using UnityEngine.InputSystem;

namespace _Player.Scripts
{
    public class PlayerManager : MonoBehaviour
    {
        [SerializeField] private List<LayerMask> playerLayers;
        
        private List<PlayerInput> players = new List<PlayerInput>();
        private PlayerInputManager playerInputManager;
        
        private const int INVALID_INDEX = -1;
        private const int RENDER_ALL_LAYERS = ~0;
        
        private static readonly Dictionary<string, int> TagToIndexMap = new Dictionary<string, int>
        {
            { "PlayerOne", 0 },
            { "PlayerTwo", 1 },
            { "PlayerThree", 2 },
            { "PlayerFour", 3 }
        };
        
        private static readonly Dictionary<int, string> IndexToTagMap = new Dictionary<int, string>
        {
            { 1, "PlayerOne" },
            { 2, "PlayerTwo" },
            { 3, "PlayerThree" },
            { 4, "PlayerFour" }
        };
        
        private void Awake()
        {
            InitializePlayerInputManager();
        }
        
        private void OnEnable()
        {
            SubscribeToPlayerJoinedEvent();
        }
        
        private void OnDisable()
        {
            UnsubscribeFromPlayerJoinedEvent();
        }
        
        private void InitializePlayerInputManager()
        {
            playerInputManager = FindFirstObjectByType<PlayerInputManager>();
            
            if (playerInputManager == null)
            {
                Debug.LogError($"[{nameof(PlayerManager)}] PlayerInputManager not found!");
            }
        }
        
        private void SubscribeToPlayerJoinedEvent()
        {
            if (playerInputManager == null)
            {
                InitializePlayerInputManager();
            }
            
            if (playerInputManager != null)
            {
                playerInputManager.onPlayerJoined += AddPlayer;
            }
            else
            {
                Debug.LogWarning($"[{nameof(PlayerManager)}] PlayerInputManager not found in OnEnable");
            }
        }
        
        private void UnsubscribeFromPlayerJoinedEvent()
        {
            if (playerInputManager != null)
            {
                playerInputManager.onPlayerJoined -= AddPlayer;
            }
        }
        
        private void AddPlayer(PlayerInput player)
        {
            if (IsPlayerAlreadyRegistered(player))
            {
                return;
            }
            
            if (IsRespawningPlayer(player, out int existingPlayerIndex))
            {
                HandleRespawningPlayer(player, existingPlayerIndex);
                return;
            }
            
            HandleNewPlayer(player);
        }
        
        private bool IsPlayerAlreadyRegistered(PlayerInput player)
        {
            return players.Contains(player);
        }
        
        private bool IsRespawningPlayer(PlayerInput player, out int existingPlayerIndex)
        {
            existingPlayerIndex = GetPlayerIndexFromTag(player.gameObject.tag);
            return existingPlayerIndex != INVALID_INDEX;
        }
        
        private void HandleRespawningPlayer(PlayerInput player, int existingPlayerIndex)
        {
            if (existingPlayerIndex < players.Count)
            {
                players[existingPlayerIndex] = player;
            }
            
            AssignPlayerLayer(player, existingPlayerIndex);
        }
        
        private void HandleNewPlayer(PlayerInput player)
        {
            players.Add(player);
            
            int playerIndex = players.Count - 1;
            
            if (!ValidatePlayerLayerExists(playerIndex))
            {
                return;
            }
            
            AssignPlayerTag(player, playerIndex);
            AssignPlayerLayer(player, playerIndex);
        }
        
        private bool ValidatePlayerLayerExists(int playerIndex)
        {
            if (playerIndex >= playerLayers.Count)
            {
                Debug.LogError($"[{nameof(PlayerManager)}] Not enough player layers! Player {players.Count} cannot be assigned a layer. Please add more layers.");
                return false;
            }
            
            return true;
        }
        
        private void AssignPlayerTag(PlayerInput player, int playerIndex)
        {
            string playerTag = GetPlayerTag(playerIndex + 1);
            player.gameObject.tag = playerTag;
            player.gameObject.name = playerTag;
        }
        
        private void AssignPlayerLayer(PlayerInput player, int playerIndex)
        {
            int layer = ConvertLayerMaskToLayer(playerLayers[playerIndex]);
            
            CinemachineCamera cinemachineCamera = player.GetComponentInChildren<CinemachineCamera>();
            Camera playerCamera = player.GetComponentInChildren<Camera>();
            CinemachineBrain cinemachineBrain = player.GetComponentInChildren<CinemachineBrain>();
            
            SetupCinemachineCamera(cinemachineCamera, layer);
            SetupPlayerCamera(playerCamera, layer, playerIndex);
            SetupCinemachineBrain(cinemachineBrain, layer);
        }
        
        private int ConvertLayerMaskToLayer(LayerMask layerMask)
        {
            return (int)Mathf.Log(layerMask.value, 2);
        }
        
        private void SetupCinemachineCamera(CinemachineCamera cinemachineCamera, int layer)
        {
            if (cinemachineCamera != null)
            {
                cinemachineCamera.gameObject.layer = layer;
            }
        }
        
        private void SetupPlayerCamera(Camera playerCamera, int layer, int playerIndex)
        {
            if (playerCamera == null)
            {
                return;
            }
            
            playerCamera.gameObject.layer = layer;
            playerCamera.cullingMask = CalculateCullingMask(playerIndex);
        }
        
        private int CalculateCullingMask(int playerIndex)
        {
            int cullingMask = RENDER_ALL_LAYERS;
            
            for (int i = 0; i < playerLayers.Count; i++)
            {
                if (i != playerIndex)
                {
                    int otherLayer = ConvertLayerMaskToLayer(playerLayers[i]);
                    cullingMask &= ~(1 << otherLayer);
                }
            }
            
            return cullingMask;
        }
        
        private void SetupCinemachineBrain(CinemachineBrain cinemachineBrain, int layer)
        {
            if (cinemachineBrain != null)
            {
                cinemachineBrain.ChannelMask = (OutputChannels)(1 << layer);
            }
        }
        
        private int GetPlayerIndexFromTag(string tag)
        {
            if (TagToIndexMap.TryGetValue(tag, out int index))
            {
                return index;
            }
            
            return INVALID_INDEX;
        }
        
        private string GetPlayerTag(int playerNumber)
        {
            if (IndexToTagMap.TryGetValue(playerNumber, out string tag))
            {
                return tag;
            }
            
            Debug.LogWarning($"[{nameof(PlayerManager)}] Invalid player number: {playerNumber}, defaulting to PlayerOne");
            return "PlayerOne";
        }
    }
}