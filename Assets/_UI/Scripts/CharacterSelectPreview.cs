using _Cars.ScriptableObjects;
using _Projectiles.ScriptableObjects;
using UnityEngine;

namespace _UI.Scripts
{
    /// <summary>
    /// Spawns a spinning car + weapon preview for the character select screen.
    /// 
    /// For singleplayer: assign carTypes + weaponTypes in Inspector, playerIndex = 0
    /// For multiplayer:  PlayerCharacterSelectPanel calls SetAssets() + SetPlayerIndex()
    ///                   then UpdatePreview() — no Inspector setup needed on prefab
    /// </summary>
    public class CharacterSelectPreview : MonoBehaviour
    {
        [Header("Assets — assign in Inspector for singleplayer, set at runtime for multiplayer")]
        [SerializeField] private CarStats[]         carTypes;
        [SerializeField] private ProjectileObject[] weaponTypes;

        [Header("Player Setup")]
        [Tooltip("0 = P1, 1 = P2, 2 = P3, 3 = P4")]
        [SerializeField] private int playerIndex = 0;

        [Header("Raw Image — assign the RawImage that displays the preview")]
        [SerializeField] private UnityEngine.UI.RawImage previewDisplay;

        [Header("Spin Settings")]
        [SerializeField] private float spinSpeed = 45f;

        [Header("Preview Layer")]
        [SerializeField] private LayerMask previewLayerMask;

        private Transform  previewRoot;
        private Transform  carMount;
        private Camera     previewCamera;
        private GameObject spawnedCar;
        private int        previewLayer;
        private bool       initialized = false;

        // ═══════════════════════════════════════════════
        //  LIFECYCLE
        // ═══════════════════════════════════════════════

        private void Awake()
        {
            previewLayer = Mathf.RoundToInt(Mathf.Log(previewLayerMask.value, 2));
        }

        private void Start()
        {
            // Only auto-spawn for singleplayer (assets assigned in Inspector)
            // Multiplayer calls UpdatePreview() directly after SetAssets()
            if (!initialized && carTypes != null && weaponTypes != null)
                UpdatePreview(0, 0);
        }

        private void Update()
        {
            if (previewRoot != null)
                previewRoot.Rotate(Vector3.up, spinSpeed * Time.deltaTime, Space.World);
        }

        private void OnDisable()
        {
            // Clean up spawned car when panel is hidden so it doesn't stack
            ClearPreview();
        }

        // ═══════════════════════════════════════════════
        //  SETUP — called by PlayerCharacterSelectPanel
        // ═══════════════════════════════════════════════

        public void SetAssets(CarStats[] cars, ProjectileObject[] weapons)
        {
            carTypes    = cars;
            weaponTypes = weapons;
        }

        public void SetPlayerIndex(int index)
        {
            if (playerIndex == index && previewRoot != null) return;
            playerIndex = index;
            previewRoot = null; // force re-resolve
            carMount    = null;
        }

        // ═══════════════════════════════════════════════
        //  RESOLVE — finds the correct preview setup
        // ═══════════════════════════════════════════════

        private bool ResolveSetup()
        {
            if (previewRoot != null) return true; // already resolved

            var manager = CharacterSelectPreviewManager.instance;
            if (manager == null)
            {
                Debug.LogWarning("[CharacterSelectPreview] No CharacterSelectPreviewManager in scene!");
                return false;
            }

            var setup = manager.GetSetup(playerIndex);
            if (setup == null)
            {
                Debug.LogWarning($"[CharacterSelectPreview] No setup for player {playerIndex}!");
                return false;
            }

            previewRoot   = setup.previewRoot;
            carMount      = setup.carMount;
            previewCamera = setup.previewCamera;

            // Assign the correct RenderTexture to the RawImage
            if (previewCamera != null && previewCamera.targetTexture != null)
            {
                if (previewDisplay == null)
                    previewDisplay = GetComponentInChildren<UnityEngine.UI.RawImage>();

                if (previewDisplay != null)
                {
                    previewDisplay.texture = previewCamera.targetTexture;
                    Debug.Log($"[CharacterSelectPreview] P{playerIndex + 1} RawImage → {previewCamera.targetTexture.name}");
                }
            }

            Debug.Log($"[CharacterSelectPreview] P{playerIndex+1} → root={setup.previewRoot.name}, carMount={setup.carMount.name}, cam={setup.previewCamera.name}");
            return true;
        }

        // ═══════════════════════════════════════════════
        //  PUBLIC API
        // ═══════════════════════════════════════════════

        public void UpdatePreview(int carIndex, int weaponIndex)
        {
            if (!ResolveSetup()) return;
            if (carTypes == null || weaponTypes == null) return;

            initialized = true;

            carIndex    = Mathf.Clamp(carIndex,    0, carTypes.Length    - 1);
            weaponIndex = Mathf.Clamp(weaponIndex, 0, weaponTypes.Length - 1);

            SpawnCar(carTypes[carIndex], weaponIndex);

            Debug.Log($"[CharacterSelectPreview] P{playerIndex + 1} preview: {carTypes[carIndex].CarName} + {weaponTypes[weaponIndex].ProjectileName}");
        }

        public void ClearPreview()
        {
            if (spawnedCar != null)
            {
                Destroy(spawnedCar);
                spawnedCar = null;
            }
        }

        // ═══════════════════════════════════════════════
        //  SPAWN
        // ═══════════════════════════════════════════════

        private void SpawnCar(CarStats car, int weaponIndex)
        {
            ClearPreview();

            if (car.CarModelPrefab == null)
            {
                Debug.LogWarning($"[CharacterSelectPreview] No car model prefab on {car.CarName}!");
                return;
            }

            if (carMount == null)
            {
                Debug.LogWarning("[CharacterSelectPreview] CarMount is null!");
                return;
            }

            spawnedCar                            = Instantiate(car.CarModelPrefab, carMount);
            spawnedCar.name                       = "PreviewCar";
            spawnedCar.transform.localPosition    = car.CarModelOffset;
            spawnedCar.transform.localEulerAngles = car.CarModelRotationEuler;
            spawnedCar.transform.localScale       = car.CarModelScale;

            SetLayerRecursively(spawnedCar, previewLayer);
            SpawnWeapon(car, weaponIndex);
        }

        private void SpawnWeapon(CarStats car, int weaponIndex)
        {
            if (spawnedCar == null) return;

            ProjectileObject weapon = weaponTypes[weaponIndex];
            if (weapon == null || weapon.WeaponModelPrefab == null) return;

            Transform weaponMount = FindDeepChild(spawnedCar.transform, "WeaponMount");
            if (weaponMount == null) return;

            GameObject model                       = Instantiate(weapon.WeaponModelPrefab, weaponMount);
            model.name                             = "PreviewWeapon";
            model.transform.localPosition          = car.GetWeaponPositionOffset(weaponIndex);
            model.transform.localEulerAngles       = car.GetWeaponRotationOffset(weaponIndex).eulerAngles;
            model.transform.localScale             = car.GetWeaponScaleOffset(weaponIndex);

            SetLayerRecursively(model, previewLayer);
        }

        // ═══════════════════════════════════════════════
        //  HELPERS
        // ═══════════════════════════════════════════════

        private void SetLayerRecursively(GameObject go, int layer)
        {
            if (layer <= 0) return;
            go.layer = layer;
            foreach (Transform child in go.transform)
                SetLayerRecursively(child.gameObject, layer);
        }

        private Transform FindDeepChild(Transform parent, string childName)
        {
            foreach (Transform child in parent)
            {
                if (child.name == childName) return child;
                Transform found = FindDeepChild(child, childName);
                if (found != null) return found;
            }
            return null;
        }
    }
}