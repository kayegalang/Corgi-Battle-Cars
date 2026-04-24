using _Cars.ScriptableObjects;
using _Effects.Scripts;
using _Gameplay.Scripts;
using _Projectiles.ScriptableObjects;
using UnityEngine;

namespace _Cars.Scripts
{
    public class CarVisualLoader : MonoBehaviour
    {
        [Header("All Available Cars (same order as CharacterSelectUI)")]
        [SerializeField] private CarStats[] carTypes;

        [Header("All Available Weapons (same order as CharacterSelectUI)")]
        [SerializeField] private ProjectileObject[] weaponTypes;

        [Header("Mount Point")]
        [SerializeField] private Transform carMount;

        private const string KEY_CAR    = "SelectedCarTypeIndex";
        private const string KEY_WEAPON = "SelectedWeaponTypeIndex";

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
            return 0;
        }

        // ═══════════════════════════════════════════════
        //  LIFECYCLE
        // ═══════════════════════════════════════════════

        private void Awake()
        {
            if (GetComponent<BotLoadoutRandomizer>() != null) return;

            if (GameplayManager.instance != null &&
                GameplayManager.instance.GetCurrentGameMode() == GameMode.Singleplayer)
                LoadVisuals();
        }

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

            CarStats         loadoutCar    = PlayerLoadout.GetCar(playerIdx);
            ProjectileObject loadoutWeapon = PlayerLoadout.GetWeapon(playerIdx);

            int carIndex    = loadoutCar    != null ? System.Array.IndexOf(carTypes,    loadoutCar)    : Mathf.Clamp(PlayerPrefs.GetInt(KEY_CAR,    0), 0, carTypes.Length    - 1);
            int weaponIndex = loadoutWeapon != null ? System.Array.IndexOf(weaponTypes, loadoutWeapon) : Mathf.Clamp(PlayerPrefs.GetInt(KEY_WEAPON, 0), 0, weaponTypes.Length - 1);

            carIndex    = Mathf.Clamp(carIndex,    0, carTypes.Length    - 1);
            weaponIndex = Mathf.Clamp(weaponIndex, 0, weaponTypes.Length - 1);

            LoadVisuals(carIndex, weaponIndex);
        }

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

            model.transform.localPosition = car.GetWeaponPositionOffset(weaponIndex);
            model.transform.localRotation = car.GetWeaponRotationOffset(weaponIndex);
            model.transform.localScale    = car.GetWeaponScaleOffset(weaponIndex);

            Transform firePoint = FindDeepChild(model.transform, "FirePoint");
            if (firePoint != null)
                GetComponent<CarShooter>()?.SetFirePoint(firePoint);
            else
                Debug.LogWarning($"[CarVisualLoader] No 'FirePoint' found in weapon prefab '{weapon.ProjectileName}'!");

            WeaponVisualBase weaponVisual = model.GetComponentInChildren<WeaponVisualBase>();
            if (weaponVisual != null)
            {
                weaponVisual.SetPlayerRoot(transform);

                HoundVisual houndVisual = weaponVisual as HoundVisual;
                houndVisual?.SetOwner(gameObject);

                GetComponent<TurretVisuals>()?.SetWeaponVisual(weaponVisual);
            }
            else
                Debug.LogWarning($"[CarVisualLoader] No WeaponVisualBase found in weapon prefab '{weapon.ProjectileName}'!");
        }

        // ═══════════════════════════════════════════════
        //  RESOLVE REFERENCES
        // ═══════════════════════════════════════════════

        private void ResolveReferences(GameObject spawnedCar)
        {
            Debug.Log($"[CarVisualLoader] ResolveReferences called on {gameObject.name} (tag: {gameObject.tag})");

            Transform car = spawnedCar.transform;

            // ── HitEffects (car flash renderers) ─────────
            HitEffects hitEffects = GetComponent<HitEffects>();
            if (hitEffects != null)
            {
                Renderer[] renderers = spawnedCar.GetComponentsInChildren<Renderer>();
                hitEffects.SetRenderers(renderers);
            }

            // ── CarColorizer (hue shift per player) ──────
            CarColorizer colorizer = GetComponent<CarColorizer>();
            Debug.Log($"[CarVisualLoader] CarColorizer found: {colorizer != null}");
            if (colorizer != null)
            {
                Renderer[] renderers = spawnedCar.GetComponentsInChildren<Renderer>();
                Debug.Log($"[CarVisualLoader] Passing {renderers.Length} renderers to CarColorizer");
                colorizer.ApplyColor(renderers);
            }

            // ── CarDeathEffects ──────────────────────────
            CarDeathEffects deathEffects = GetComponent<CarDeathEffects>();
            if (deathEffects != null)
            {
                ParticleSystem smoke   = FindDeepChild(car, "SmokeEffect")?.GetComponent<ParticleSystem>();
                GameObject     flame   = FindDeepChild(car, "FlameEffect")?.gameObject;
                GameObject     nameTag = FindDeepChild(car, "NameTagCanvas")?.gameObject;
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
                Transform      bodyRoot  = spawnedCar.transform;
                ParticleSystem dustLeft  = FindDeepChild(car, "DustLeft")?.GetComponent<ParticleSystem>();
                ParticleSystem dustRight = FindDeepChild(car, "DustRight")?.GetComponent<ParticleSystem>();
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
            GetComponent<_Bot.Scripts.BotAI>()?.SetFirePoint(
                FindDeepChild(spawnedCar.transform, "FirePoint") ??
                FindDeepChild(transform, "FirePoint"));
        }

        // ═══════════════════════════════════════════════
        //  HELPERS
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