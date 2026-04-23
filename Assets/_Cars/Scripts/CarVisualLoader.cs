using _Cars.ScriptableObjects;
using _Effects.Scripts;
using _Gameplay.Scripts;
using _Projectiles.ScriptableObjects;
using UnityEngine;

namespace _Cars.Scripts
{
    /// <summary>
    /// Spawns the selected car and weapon models at runtime based on PlayerPrefs,
    /// then rewires all component references that point into the spawned prefab.
    ///
    /// CAR PREFAB STRUCTURE (e.g. KorgiKart):
    ///   KorgiKart
    ///     ├── [mesh children]
    ///     ├── Corgi
    ///     ├── SmokeEffect         (ParticleSystem)
    ///     ├── FlameEffect         (GameObject)
    ///     ├── ZoomiesSpeedTrail   (ParticleSystem)
    ///     ├── Back Left Wheel
    ///     ├── Back Right Wheel
    ///     ├── Front Left Wheel
    ///     ├── Front Right Wheel
    ///     ├── NameTagCanvas
    ///     └── WeaponMount
    ///           └── Mount
    ///
    /// WEAPON PREFAB STRUCTURE (e.g. bArK-47):
    ///   bArK-47
    ///     ├── [mesh children]
    ///     └── FirePoint
    ///
    /// Add CarVisualLoader to DefaultPlayer root.
    /// </summary>
    public class CarVisualLoader : MonoBehaviour
    {
        [Header("All Available Cars (same order as CharacterSelectUI)")]
        [SerializeField] private CarStats[] carTypes;

        [Header("All Available Weapons (same order as CharacterSelectUI)")]
        [SerializeField] private ProjectileObject[] weaponTypes;

        [Header("Mount Point")]
        [Tooltip("Empty GameObject under Visuals — the car prefab spawns here")]
        [SerializeField] private Transform carMount;

        private const string KEY_CAR    = "SelectedCarTypeIndex";
        private const string KEY_WEAPON = "SelectedWeaponTypeIndex";

        // Maps player tags to PlayerLoadout indices
        private static readonly System.Collections.Generic.Dictionary<string, int> TagToIndex
            = new System.Collections.Generic.Dictionary<string, int>
        {
            { "PlayerOne",   0 },
            { "PlayerTwo",   1 },
            { "PlayerThree", 2 },
            { "PlayerFour",  3 },
        };

        private int GetPlayerIndex()
        {
            if (TagToIndex.TryGetValue(gameObject.tag, out int index))
                return index;
            return 0; // default to P1 for bots or untagged
        }

        // ═══════════════════════════════════════════════
        //  LIFECYCLE
        // ═══════════════════════════════════════════════

        private void Awake()
        {
            // In singleplayer tag is already correct — load immediately
            // In multiplayer SpawnManager calls LoadForCurrentTag() after setting the tag
            if (GetComponent<BotLoadoutRandomizer>() != null) return;

            if (GameplayManager.instance != null &&
                GameplayManager.instance.GetCurrentGameMode() == GameMode.Singleplayer)
                LoadVisuals();
        }

        /// <summary>Called by SpawnManager after SetPlayerIdentity() so tag is correct.</summary>
        public void LoadForCurrentTag()
        {
            LoadVisuals();
        }

        // ═══════════════════════════════════════════════
        //  LOAD
        // ═══════════════════════════════════════════════

        public void LoadVisuals()
        {
            int playerIdx = GetPlayerIndex();

            // Try PlayerLoadout first (set by character select)
            CarStats         loadoutCar    = PlayerLoadout.GetCar(playerIdx);
            ProjectileObject loadoutWeapon = PlayerLoadout.GetWeapon(playerIdx);

            // Fall back to PlayerPrefs for backwards compatibility
            int carIndex    = loadoutCar    != null ? System.Array.IndexOf(carTypes,    loadoutCar)    : Mathf.Clamp(PlayerPrefs.GetInt(KEY_CAR,    0), 0, carTypes.Length    - 1);
            int weaponIndex = loadoutWeapon != null ? System.Array.IndexOf(weaponTypes, loadoutWeapon) : Mathf.Clamp(PlayerPrefs.GetInt(KEY_WEAPON, 0), 0, weaponTypes.Length - 1);

            carIndex    = Mathf.Clamp(carIndex,    0, carTypes.Length    - 1);
            weaponIndex = Mathf.Clamp(weaponIndex, 0, weaponTypes.Length - 1);

            LoadVisuals(carIndex, weaponIndex);
        }

        /// <summary>Called directly by BotLoadoutRandomizer with explicit indices.</summary>
        public void LoadVisuals(int carIndex, int weaponIndex)
        {
            carIndex    = Mathf.Clamp(carIndex,    0, carTypes.Length    - 1);
            weaponIndex = Mathf.Clamp(weaponIndex, 0, weaponTypes.Length - 1);

            CarStats         selectedCar    = carTypes[carIndex];
            ProjectileObject selectedWeapon = weaponTypes[weaponIndex];

            GameObject spawnedCar = SpawnCarModel(selectedCar);
            if (spawnedCar == null) return;

            SpawnWeaponModel(selectedWeapon, selectedCar, weaponIndex, spawnedCar);
            ResolveReferences(spawnedCar);

            Debug.Log($"[CarVisualLoader] Loaded: {selectedCar.CarName} + {selectedWeapon.ProjectileName}");
        }

        // ═══════════════════════════════════════════════
        //  SPAWN CAR
        // ═══════════════════════════════════════════════

        private GameObject SpawnCarModel(CarStats car)
        {
            if (carMount == null)
            {
                Debug.LogWarning("[CarVisualLoader] CarMount not assigned!");
                return null;
            }

            if (car.CarModelPrefab == null)
            {
                Debug.LogWarning($"[CarVisualLoader] No car model prefab on {car.CarName}!");
                return null;
            }

            ClearDynamicChildren(carMount);

            GameObject model                 = Instantiate(car.CarModelPrefab, carMount);
            model.name                       = "CarModel_Dynamic";
            model.transform.localPosition    = car.CarModelOffset;
            model.transform.localEulerAngles = car.CarModelRotationEuler;
            model.transform.localScale       = car.CarModelScale;

            Debug.Log($"[CarVisualLoader] Car spawned!" +
                      $"\n  Rotation requested: {car.CarModelRotationEuler}" +
                      $"\n  Rotation applied:   {model.transform.localEulerAngles}" +
                      $"\n  Prefab rotation:    {car.CarModelPrefab.transform.eulerAngles}");

            return model;
        }

        // ═══════════════════════════════════════════════
        //  SPAWN WEAPON
        // ═══════════════════════════════════════════════

        private void SpawnWeaponModel(ProjectileObject weapon, CarStats car, int weaponIndex, GameObject spawnedCar)
        {
            Transform weaponMount = FindDeepChild(spawnedCar.transform, "WeaponMount");
            if (weaponMount == null)
            {
                Debug.LogWarning("[CarVisualLoader] No 'WeaponMount' found inside car prefab!");
                return;
            }

            if (weapon.WeaponModelPrefab == null)
            {
                Debug.LogWarning($"[CarVisualLoader] No weapon model prefab on {weapon.ProjectileName}!");
                return;
            }

            ClearDynamicChildren(weaponMount);

            GameObject model = Instantiate(weapon.WeaponModelPrefab, weaponMount);
            model.name = "WeaponModel_Dynamic";

            // Apply the car's per-weapon offset — each car defines where each weapon sits
            model.transform.localPosition = car.GetWeaponPositionOffset(weaponIndex);
            model.transform.localRotation = car.GetWeaponRotationOffset(weaponIndex);
            model.transform.localScale    = car.GetWeaponScaleOffset(weaponIndex);

            // Hand FirePoint to CarShooter
            Transform firePoint = FindDeepChild(model.transform, "FirePoint");
            if (firePoint != null)
                GetComponent<CarShooter>()?.SetFirePoint(firePoint);
            else
                Debug.LogWarning($"[CarVisualLoader] No 'FirePoint' found in weapon prefab '{weapon.ProjectileName}'!");

            // Wire TurretVisuals to the weapon's visual component
            WeaponVisualBase weaponVisual = model.GetComponentInChildren<WeaponVisualBase>();
            if (weaponVisual != null)
            {
                weaponVisual.SetPlayerRoot(transform);

                // Pass owner for laser damage attribution
                HoundVisual houndVisual = weaponVisual as HoundVisual;
                houndVisual?.SetOwner(gameObject);

                GetComponent<TurretVisuals>()?.SetWeaponVisual(weaponVisual);
            }
            else
                Debug.LogWarning($"[CarVisualLoader] No WeaponVisualBase found in weapon prefab '{weapon.ProjectileName}'! Add BArK47Visual or HoundVisual to the prefab.");
        }

        // ═══════════════════════════════════════════════
        //  RESOLVE REFERENCES
        //  Rewires all component references that point into
        //  the spawned car prefab after it's instantiated
        // ═══════════════════════════════════════════════

        private void ResolveReferences(GameObject spawnedCar)
        {
            Transform car = spawnedCar.transform;

            // ── HitEffects (car flash renderers) ─────────
            HitEffects hitEffects = GetComponent<HitEffects>();
            if (hitEffects != null)
            {
                Renderer[] renderers = spawnedCar.GetComponentsInChildren<Renderer>();
                hitEffects.SetRenderers(renderers);
            }

            // ── CarDeathEffects ──────────────────────────
            CarDeathEffects deathEffects = GetComponent<CarDeathEffects>();
            if (deathEffects != null)
            {
                ParticleSystem smoke    = FindDeepChild(car, "SmokeEffect")?.GetComponent<ParticleSystem>();
                GameObject     flame    = FindDeepChild(car, "FlameEffect")?.gameObject;
                GameObject     nameTag  = FindDeepChild(car, "NameTagCanvas")?.gameObject;
                deathEffects.SetReferences(smoke, flame, nameTag);
            }

            // ── WheelVisuals ─────────────────────────────
            WheelVisuals wheels = GetComponent<WheelVisuals>();
            if (wheels != null)
            {
                Transform fl = FindDeepChild(car, "Front Left Wheel Model");
                Transform fr = FindDeepChild(car, "Front Right Wheel Model");
                Transform rl = FindDeepChild(car, "Back Left Wheel Model");
                Transform rr = FindDeepChild(car, "Back Right Wheel Model");
                wheels.SetWheels(fl, fr, rl, rr);
            }

            // ── DriftEffects ─────────────────────────────
            DriftEffects drift = GetComponent<DriftEffects>();
            if (drift != null)
            {
                // carBodyRoot is the spawned car model root itself
                Transform      bodyRoot   = spawnedCar.transform;
                ParticleSystem dustLeft   = FindDeepChild(car, "DustLeft")?.GetComponent<ParticleSystem>();
                ParticleSystem dustRight  = FindDeepChild(car, "DustRight")?.GetComponent<ParticleSystem>();
                drift.SetReferences(bodyRoot, dustLeft, dustRight);
            }

            // ── CarController (zoomies particles) ────────
            CarController controller = GetComponent<CarController>();
            if (controller != null)
            {
                ParticleSystem zoomies = FindDeepChild(car, "ZoomiesSpeedTrail")?.GetComponent<ParticleSystem>();
                if (zoomies != null)
                    controller.SetZoomiesParticles(zoomies);
            }

            // ── BotController (zoomies particles) ────────
            _Bot.Scripts.BotController botController = GetComponent<_Bot.Scripts.BotController>();
            if (botController != null)
            {
                ParticleSystem zoomies = FindDeepChild(car, "ZoomiesSpeedTrail")?.GetComponent<ParticleSystem>();
                if (zoomies != null)
                    botController.SetZoomiesParticles(zoomies);
            }

            // ── BotAI FirePoint ───────────────────────────
            // Bots need FirePoint wired after weapon spawns just like CarShooter
            GetComponent<_Bot.Scripts.BotAI>()?.SetFirePoint(
                FindDeepChild(spawnedCar.transform, "FirePoint") ??
                FindDeepChild(transform, "FirePoint"));
        }

        // ═══════════════════════════════════════════════
        //  SMART CLEAR — only removes _Dynamic objects
        // ═══════════════════════════════════════════════

        private void ClearDynamicChildren(Transform parent)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                GameObject child = parent.GetChild(i).gameObject;
                if (child.name.EndsWith("_Dynamic"))
                    Destroy(child);
            }
        }

        // ═══════════════════════════════════════════════
        //  HELPERS
        // ═══════════════════════════════════════════════

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