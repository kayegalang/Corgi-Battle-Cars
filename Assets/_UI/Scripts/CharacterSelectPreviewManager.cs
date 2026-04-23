using UnityEngine;

namespace _UI.Scripts
{
    /// <summary>
    /// Singleton that lives in the MainMenu scene.
    /// Holds the 4 preview setups (root, carMount, camera) for each player.
    /// CharacterSelectPreview finds its correct setup by player index at runtime.
    /// </summary>
    public class CharacterSelectPreviewManager : MonoBehaviour
    {
        public static CharacterSelectPreviewManager instance;

        [System.Serializable]
        public class PreviewSetup
        {
            public Transform previewRoot;
            public Transform carMount;
            public Camera    previewCamera;
        }

        [Header("One entry per player — index 0 = P1, 1 = P2, etc.")]
        [SerializeField] private PreviewSetup[] setups = new PreviewSetup[4];

        [Header("Spacing")]
        [Tooltip("Base world position for all preview areas")]
        [SerializeField] private Vector3 basePosition = new Vector3(1000f, 0f, 0f);
        [Tooltip("How far apart each preview root is spaced on X axis")]
        [SerializeField] private float spacing = 50f;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;

            // Auto-space each preview root and move its camera by the same offset
            for (int i = 0; i < setups.Length; i++)
            {
                if (setups[i] == null) continue;

                float xOffset = i * spacing;

                if (setups[i].previewRoot != null)
                    setups[i].previewRoot.localPosition = new Vector3(xOffset, 0f, 0f);

                if (setups[i].previewCamera != null)
                {
                    Vector3 camLocal = setups[i].previewCamera.transform.localPosition;
                    setups[i].previewCamera.transform.localPosition = new Vector3(
                        xOffset + camLocal.x,  // preserve original X offset relative to root
                        camLocal.y,
                        camLocal.z);
                }

                Debug.Log($"[CharacterSelectPreviewManager] P{i + 1} spaced to X={xOffset}");
            }
        }

        public PreviewSetup GetSetup(int playerIndex)
        {
            if (playerIndex < 0 || playerIndex >= setups.Length)
            {
                Debug.LogWarning($"[CharacterSelectPreviewManager] Invalid player index: {playerIndex}");
                return null;
            }
            return setups[playerIndex];
        }
    }
}