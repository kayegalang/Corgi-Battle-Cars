using System.Collections.Generic;
using UnityEngine;

namespace _Gameplay.Scripts
{
    public class PlayerRegistry : MonoBehaviour
    {
        public static PlayerRegistry instance;
        
        private List<string> playerTags = new List<string>();
        
        private void Awake()
        {
            InitializeSingleton();
        }
        
        private void InitializeSingleton()
        {
            if (instance == null)
            {
                instance = this;
            }
            else if (instance != this)
            {
                Destroy(this);
            }
        }
        
        public void RegisterPlayer(string playerTag)
        {
            if (string.IsNullOrEmpty(playerTag))
            {
                Debug.LogWarning($"[{nameof(PlayerRegistry)}] Cannot register player with null or empty tag");
                return;
            }
            
            if (playerTags.Contains(playerTag))
            {
                Debug.LogWarning($"[{nameof(PlayerRegistry)}] Player {playerTag} already registered");
                return;
            }
            
            playerTags.Add(playerTag);
            Debug.Log($"[{nameof(PlayerRegistry)}] Player added: {playerTag}, Total players: {playerTags.Count}");
        }
        
        public void UnregisterPlayer(string playerTag)
        {
            if (playerTags.Contains(playerTag))
            {
                playerTags.Remove(playerTag);
                Debug.Log($"[{nameof(PlayerRegistry)}] Player removed: {playerTag}");
            }
        }
        
        public List<string> GetAllPlayerTags()
        {
            return new List<string>(playerTags);
        }
        
        public int GetPlayerCount()
        {
            return playerTags.Count;
        }
        
        public bool IsPlayerRegistered(string playerTag)
        {
            return playerTags.Contains(playerTag);
        }
        
        public bool HasPlayers()
        {
            return playerTags.Count > 0;
        }
        
        public void Clear()
        {
            playerTags.Clear();
            Debug.Log($"[{nameof(PlayerRegistry)}] Player registry cleared");
        }
    }
}