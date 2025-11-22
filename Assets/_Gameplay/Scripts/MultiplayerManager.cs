using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace _Gameplay.Scripts
{
    public class MultiplayerManager : MonoBehaviour
    {
        public static MultiplayerManager instance;

        [Header("UI Slots (size 4)")]
        [SerializeField] private RawImage[] slotImages;
        [SerializeField] private TMP_Text[] slotTexts;

        [Header("Colors")]
        [SerializeField] private Color waitingColor = Color.gray;
        [SerializeField] private Color joinedColor = Color.green;

        [SerializeField] private GameObject characterSelectionPanel;
        [SerializeField] private GameObject joiningPanel;

        private PlayerInputManager inputManager;

        private int expectedPlayers = 2;
        private int joinedPlayers = 0;

        private readonly List<PlayerInput> playerList = new();
        private readonly HashSet<GameObject> processedPlayers = new();

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            inputManager = FindAnyObjectByType<PlayerInputManager>();
            SetupSlots();
        }

        private void OnEnable()
        {
            if (inputManager != null)
                inputManager.onPlayerJoined += HandlePlayerJoined;
        }

        private void OnDisable()
        {
            if (inputManager != null)
                inputManager.onPlayerJoined -= HandlePlayerJoined;
        }

        public void SelectPlayerCount(int count)
        {
            expectedPlayers = Mathf.Clamp(count, 2, 4);
            joinedPlayers = 0;
            playerList.Clear();

            SetupSlots();

            GameplayManager.instance.SetGameMode(GameMode.Multiplayer);
        }

        private void SetupSlots()
        {
            expectedPlayers = Mathf.Clamp(expectedPlayers, 2, 4);

            for (int i = 0; i < slotImages.Length; i++)
            {
                bool activeSlot = i < expectedPlayers;

                slotImages[i].gameObject.SetActive(activeSlot);
                slotTexts[i].gameObject.SetActive(activeSlot);

                if (activeSlot)
                {
                    slotImages[i].color = waitingColor;
                    slotTexts[i].text = "Waiting...";
                }
            }
        }

        private void HandlePlayerJoined(PlayerInput player)
        {
            // ONLY handle players in multiplayer mode
            if (GameplayManager.instance != null && 
                GameplayManager.instance.GetGameMode() != GameMode.Multiplayer)
            {
                Debug.Log("Player joined but not in multiplayer mode, ignoring");
                return;
            }

            if (joinedPlayers >= expectedPlayers)
            {
                Destroy(player.gameObject);
                return;
            }

            // Update UI
            slotImages[joinedPlayers].color = joinedColor;
            slotTexts[joinedPlayers].text = "Joined!";

            // Store root persistently
            GameObject root = player.transform.root.gameObject;
            DontDestroyOnLoad(root);
            
            // Delay disabling by one frame to let Awake/Start complete
            StartCoroutine(DisablePlayerGameplayDelayed(root));
            
            playerList.Add(player);
            joinedPlayers++;

            if (joinedPlayers == expectedPlayers)
                OnAllPlayersJoined();
        }

        private IEnumerator DisablePlayerGameplayDelayed(GameObject playerRoot)
        {
            yield return null;
            DisablePlayerGameplay(playerRoot);
        }

        private void DisablePlayerGameplay(GameObject playerRoot)
        {
            Debug.Log($"Disabling gameplay for {playerRoot.name}");

            var cameras = playerRoot.GetComponentsInChildren<Camera>(true);
            foreach (var cam in cameras)
            {
                cam.enabled = false;
                Debug.Log($"Disabled camera: {cam.name}");
            }

            var canvases = playerRoot.GetComponentsInChildren<Canvas>(true);
            foreach (var canvas in canvases)
            {
                canvas.enabled = false;
                Debug.Log($"Disabled canvas: {canvas.name}");
            }

            var input = playerRoot.GetComponentInChildren<PlayerInput>(true);
            if (input != null && input.actions != null)
            {
                input.DeactivateInput();
                Debug.Log($"Deactivated input for {playerRoot.name}");
            }

            var characterController = playerRoot.GetComponentInChildren<CharacterController>(true);
            if (characterController != null)
                characterController.enabled = false;

            var rb = playerRoot.GetComponentInChildren<Rigidbody>(true);
            if (rb != null)
            {
                rb.isKinematic = true;
                Debug.Log($"Set Rigidbody kinematic for {playerRoot.name}");
            }

            var carControllers = playerRoot.GetComponentsInChildren<MonoBehaviour>(true);
            foreach (var script in carControllers)
            {
                if (script.GetType().Name == "CarController")
                {
                    script.enabled = false;
                    Debug.Log($"Disabled CarController on {playerRoot.name}");
                }
            }
        }

        public void EnablePlayerGameplay(GameObject playerRoot)
        {
            Debug.Log($"Enabling gameplay for {playerRoot.name}");

            var cameras = playerRoot.GetComponentsInChildren<Camera>(true);
            foreach (var cam in cameras)
                cam.enabled = true;

            var canvases = playerRoot.GetComponentsInChildren<Canvas>(true);
            foreach (var canvas in canvases)
                canvas.enabled = true;

            var input = playerRoot.GetComponentInChildren<PlayerInput>(true);
            if (input != null && input.actions != null)
            {
                input.ActivateInput();
            }

            var characterController = playerRoot.GetComponentInChildren<CharacterController>(true);
            if (characterController != null)
                characterController.enabled = true;

            var rb = playerRoot.GetComponentInChildren<Rigidbody>(true);
            if (rb != null)
            {
                rb.isKinematic = false;
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            var carControllers = playerRoot.GetComponentsInChildren<MonoBehaviour>(true);
            foreach (var script in carControllers)
            {
                if (script.GetType().Name == "CarController")
                {
                    script.enabled = true;
                }
            }
        }

        private void OnAllPlayersJoined()
        {
            characterSelectionPanel.SetActive(true);
            joiningPanel.SetActive(false);
        }

        public void StartMultiplayerMatch()
        {
            GameplayManager.instance.SetGameMode(GameMode.Multiplayer);
            GameplayManager.instance.StartGame();
        }

        public int GetPlayerCount() => expectedPlayers;

        public List<GameObject> GetJoinedPlayerRoots()
        {
            List<GameObject> roots = new();
            foreach (var p in playerList)
            {
                if (p != null)
                {
                    GameObject root = p.transform.root.gameObject;
                    if (!roots.Contains(root))
                    {
                        roots.Add(root);
                        Debug.Log($"GetJoinedPlayerRoots: Adding {root.name}");
                    }
                    else
                    {
                        Debug.LogWarning($"GetJoinedPlayerRoots: Duplicate root detected: {root.name}");
                    }
                }
            }
            Debug.Log($"GetJoinedPlayerRoots: Returning {roots.Count} player roots");
            return roots;
        }

        public void ClearJoinedPlayers()
        {
            joinedPlayers = 0;
            playerList.Clear();
        }
    }
}