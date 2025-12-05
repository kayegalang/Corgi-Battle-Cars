using System.Collections.Generic;
using UnityEngine;

namespace _Utilities.Scripts
{
    public static class CameraUtility
    {
        private static readonly int[] PlayerLayers = { 6, 7, 8, 9 };
        
        public static Camera[] FindPlayerCameras()
        {
            Camera[] allCameras = Object.FindObjectsByType<Camera>(FindObjectsSortMode.None);
            
            List<Camera> playerCameras = new List<Camera>();
            
            foreach (Camera cam in allCameras)
            {
                if (cam != null && IsPlayerLayer(cam.gameObject.layer))
                {
                    playerCameras.Add(cam);
                }
            }
            
            SortCamerasByPlayerTags(playerCameras);
            
            return playerCameras.ToArray();
        }

        private static void SortCamerasByPlayerTags(List<Camera> playerCameras)
        {
            playerCameras.Sort((a, b) => 
            {
                string tagA = a.transform.root.tag;
                string tagB = b.transform.root.tag;
                return string.Compare(tagA, tagB, System.StringComparison.Ordinal);
            });
        }
        
        private static bool IsPlayerLayer(int layer)
        {
            return layer >= PlayerLayers[0] && layer <= PlayerLayers[^1];
        }
        
        public static int[] GetPlayerLayers()
        {
            return PlayerLayers;
        }
        
        public static Camera FindCameraOnLayer(int layer)
        {
            Camera[] allCameras = Object.FindObjectsByType<Camera>(FindObjectsSortMode.None);
            
            foreach (Camera cam in allCameras)
            {
                if (cam != null && cam.gameObject.layer == layer)
                {
                    return cam;
                }
            }
            
            return null;
        }
    }
}