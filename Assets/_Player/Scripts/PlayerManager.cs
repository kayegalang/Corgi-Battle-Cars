using System.Collections.Generic;
using UnityEngine;
using Unity.Cinemachine;
using UnityEngine.InputSystem;

namespace _Player.Scripts
{
    public class PlayerManager : MonoBehaviour
    {
        [Header("Per-player camera layers (size 4)")]
        [SerializeField] private List<LayerMask> playerLayers;

        /// <summary>
        /// Called by SpawnManager AFTER moving players into the map scene.
        /// Sets up Cinemachine + Camera split-screen channels/layers/follow targets.
        /// playerIndex is 1-based (Player1 = index 1).
        /// </summary>
        public void SetupPlayer(PlayerInput player, int playerIndex)
        {
            if (player == null)
            {
                Debug.LogWarning("PlayerManager.SetupPlayer called with null PlayerInput.");
                return;
            }

            if (playerIndex < 1 || playerIndex > playerLayers.Count)
            {
                Debug.LogWarning($"Player index {playerIndex} out of range for playerLayers.");
                return;
            }

            Transform root = player.transform.root;
            Transform followTarget = player.transform;

            var cinemachineCam = root.GetComponentInChildren<CinemachineCamera>(true);
            var regularCam     = root.GetComponentInChildren<Camera>(true);

            if (cinemachineCam == null || regularCam == null)
            {
                Debug.LogWarning($"Missing cameras on player root: {root.name}");
                return;
            }

            // Convert LayerMask into layer int
            int layerToAdd = (int)Mathf.Log(playerLayers[playerIndex - 1].value, 2);

            // Put BOTH cameras on the same player layer
            cinemachineCam.gameObject.layer = layerToAdd;
            regularCam.gameObject.layer = layerToAdd;

            // Brain channel mask must match Cinemachine output
            var brain = regularCam.GetComponent<CinemachineBrain>();
            if (brain != null)
            {
                brain.ChannelMask = (OutputChannels)(1 << (playerIndex - 1));
            }

            cinemachineCam.OutputChannel = (OutputChannels)(1 << (playerIndex - 1));

            // Ensure this player’s camera only sees their layer + default world
            regularCam.cullingMask |= 1 << layerToAdd;

            // Assign follow/look targets
            cinemachineCam.Follow = followTarget;
            cinemachineCam.LookAt = followTarget;
        }
    }
}