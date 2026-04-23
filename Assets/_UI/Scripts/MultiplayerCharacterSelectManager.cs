using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using _Cars.ScriptableObjects;
using _Projectiles.ScriptableObjects;
using _Gameplay.Scripts;
using _UI.Scripts;

namespace _UI.Scripts
{
    /// <summary>
    /// Placed on the CharacterSelectionPanel.
    /// In singleplayer: activates the existing CharacterSelectUI panel.
    /// In multiplayer: builds a split-screen panel per player.
    /// </summary>
    public class MultiplayerCharacterSelectManager : MonoBehaviour
    {
        // ═══════════════════════════════════════════════
        //  INSPECTOR REFERENCES
        // ═══════════════════════════════════════════════

        [Header("Singleplayer")]
        [Tooltip("The child panel that has CharacterSelectUI on it")]
        [SerializeField] private GameObject singleplayerPanel;

        [Header("Multiplayer")]
        [Tooltip("Prefab for a single player's character select panel")]
        [SerializeField] private GameObject playerPanelPrefab;
        [Tooltip("Transform to parent the player panels under — drag CharacterSelectionPanel itself here")]
        [SerializeField] private Transform  panelsParent;
        [Tooltip("Panel to show when going back — same as CharacterSelectUI.previousPanel")]
        [SerializeField] private GameObject previousPanel;
        [Tooltip("The CharacterSelectionPanel itself — gets hidden when going back")]
        [SerializeField] private GameObject characterSelectionPanel;

        [Header("Assets")]
        [SerializeField] private CarStats[]         carTypes;
        [SerializeField] private ProjectileObject[] weaponTypes;

        // ═══════════════════════════════════════════════
        //  RUNTIME STATE
        // ═══════════════════════════════════════════════

        private readonly List<PlayerCharacterSelectPanel> panels       = new List<PlayerCharacterSelectPanel>();
        private readonly HashSet<int>                     readyPlayers = new HashSet<int>();

        private int  totalPlayers  = 1;
        private bool isMultiplayer = false;

        private static readonly Rect[] ViewportRects1 = {
            new Rect(0f, 0f, 1f, 1f)
        };

        private static readonly Rect[] ViewportRects2 = {
            new Rect(0f,   0f, 0.5f, 1f),  // Player 1 — left
            new Rect(0.5f, 0f, 0.5f, 1f)   // Player 2 — right
        };

        private static readonly Rect[] ViewportRects4 = {
            new Rect(0f,   0.5f, 0.5f, 0.5f),  // Player 1 — top-left
            new Rect(0.5f, 0.5f, 0.5f, 0.5f),  // Player 2 — top-right
            new Rect(0f,   0f,   0.5f, 0.5f),  // Player 3 — bottom-left
            new Rect(0.5f, 0f,   0.5f, 0.5f)   // Player 4 — bottom-right
        };

        private static readonly string[] PlayerTags = {
            "PlayerOne", "PlayerTwo", "PlayerThree", "PlayerFour"
        };

        // ═══════════════════════════════════════════════
        //  UNITY LIFECYCLE
        // ═══════════════════════════════════════════════

        private void OnEnable()
        {
            panels.Clear();
            readyPlayers.Clear();

            totalPlayers  = 1;
            isMultiplayer = false;

            if (GameplayManager.instance != null)
            {
                var mode = GameplayManager.instance.GetCurrentGameMode();
                if (mode == GameMode.Multiplayer)
                {
                    isMultiplayer = true;
                    totalPlayers  = Mathf.Max(2, PlayerInput.all.Count);
                }
            }

            if (isMultiplayer)
            {
                if (singleplayerPanel != null) singleplayerPanel.SetActive(false);

                var guard = FindFirstObjectByType<PlayerOneUIGuard>();
                guard?.SetAllowAllDevices(true);

                BuildMultiplayerPanels();
            }
            else
            {
                if (singleplayerPanel != null) singleplayerPanel.SetActive(true);
            }
        }

        private void OnDisable()
        {
            var guard = FindFirstObjectByType<PlayerOneUIGuard>();
            guard?.SetAllowAllDevices(false);

            // Hide dividers when leaving character select
            SplitScreenDivider.instance?.SetVisible(false);

            DestroyMultiplayerPanels();
        }

        // ═══════════════════════════════════════════════
        //  BUILD MULTIPLAYER PANELS
        // ═══════════════════════════════════════════════

        private void BuildMultiplayerPanels()
        {
            if (playerPanelPrefab == null)
            {
                Debug.LogError("[MultiplayerCharacterSelectManager] playerPanelPrefab not assigned!");
                return;
            }

            if (panelsParent == null)
            {
                Debug.LogError("[MultiplayerCharacterSelectManager] panelsParent not assigned!");
                return;
            }

            Rect[] viewportRects = GetViewportRects(totalPlayers);

            for (int i = 0; i < totalPlayers; i++)
            {
                GameObject panelGO = Instantiate(playerPanelPrefab, panelsParent);
                panelGO.name = $"PlayerPanel_{i + 1}";

                RectTransform rt = panelGO.GetComponent<RectTransform>();
                if (rt != null)
                {
                    Rect vp      = viewportRects[i];
                    rt.anchorMin = new Vector2(vp.x,            vp.y);
                    rt.anchorMax = new Vector2(vp.x + vp.width, vp.y + vp.height);
                    rt.offsetMin = Vector2.zero;
                    rt.offsetMax = Vector2.zero;
                }

                var panel = panelGO.GetComponent<PlayerCharacterSelectPanel>();
                if (panel == null)
                {
                    Debug.LogError("[MultiplayerCharacterSelectManager] playerPanelPrefab has no PlayerCharacterSelectPanel component!");
                    continue;
                }

                PlayerInput playerInput = GetPlayerInput(i);

                panel.Initialize(i, PlayerTags[i], carTypes, weaponTypes, playerInput);
                panel.OnPlayerReady   += OnPlayerReady;
                panel.OnPlayerUnready += OnPlayerUnready;
                if (i == 0) panel.OnPlayerOneBack += GoBackToJoinScreen;

                panels.Add(panel);
            }

            // Draw divider lines between player panels
            SplitScreenDivider.instance?.Setup(totalPlayers);
        }

        private PlayerInput GetPlayerInput(int playerIndex)
        {
            if (playerIndex < PlayerInput.all.Count)
                return PlayerInput.all[playerIndex];

            return null;
        }

        private void DestroyMultiplayerPanels()
        {
            foreach (var panel in panels)
            {
                if (panel == null) continue;
                panel.OnPlayerReady   -= OnPlayerReady;
                panel.OnPlayerUnready -= OnPlayerUnready;
                panel.OnPlayerOneBack -= GoBackToJoinScreen;
                Destroy(panel.gameObject);
            }
            panels.Clear();
            readyPlayers.Clear();
        }

        // ═══════════════════════════════════════════════
        //  READY TRACKING
        // ═══════════════════════════════════════════════

        private void OnPlayerReady(int playerIndex)
        {
            readyPlayers.Add(playerIndex);
            Debug.Log($"[MultiplayerCharacterSelectManager] Player {playerIndex + 1} ready! ({readyPlayers.Count}/{totalPlayers})");

            if (readyPlayers.Count >= totalPlayers)
                AdvanceToMapSelection();
        }

        private void OnPlayerUnready(int playerIndex)
        {
            readyPlayers.Remove(playerIndex);
            Debug.Log($"[MultiplayerCharacterSelectManager] Player {playerIndex + 1} un-readied. ({readyPlayers.Count}/{totalPlayers})");
        }

        // ═══════════════════════════════════════════════
        //  BACK TO JOIN SCREEN
        // ═══════════════════════════════════════════════

        private void GoBackToJoinScreen()
        {
            DestroyMultiplayerPanels();

            var guard = FindFirstObjectByType<PlayerOneUIGuard>();
            guard?.SetAllowAllDevices(false);

            if (previousPanel != null)
                previousPanel.SetActive(true);

            if (characterSelectionPanel != null)
                characterSelectionPanel.SetActive(false);
        }

        // ═══════════════════════════════════════════════
        //  ADVANCE TO MAP SELECTION
        // ═══════════════════════════════════════════════

        private void AdvanceToMapSelection()
        {
            Debug.Log("[MultiplayerCharacterSelectManager] All players ready! Advancing to map selection.");

            DestroyMultiplayerPanels();

            var guard = FindFirstObjectByType<PlayerOneUIGuard>();
            guard?.SetAllowAllDevices(false);

            var mapSelector = FindFirstObjectByType<MapSelector>(FindObjectsInactive.Include);
            if (mapSelector != null)
            {
                mapSelector.gameObject.SetActive(true);
                gameObject.SetActive(false);
            }
            else
            {
                Debug.LogWarning("[MultiplayerCharacterSelectManager] MapSelector not found in scene!");
            }
        }

        // ═══════════════════════════════════════════════
        //  VIEWPORT HELPERS
        // ═══════════════════════════════════════════════

        private Rect[] GetViewportRects(int playerCount)
        {
            if (playerCount <= 1) return ViewportRects1;
            if (playerCount == 2) return ViewportRects2;
            return ViewportRects4;
        }
    }
}