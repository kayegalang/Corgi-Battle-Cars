using UnityEngine;
using UnityEngine.UI;
using _Cars.ScriptableObjects;
using _Projectiles.ScriptableObjects;
using TMPro;

namespace _UI.Scripts
{
    public class CharacterSelectUI : MonoBehaviour
    {
        // ═══════════════════════════════════════════════
        //  INSPECTOR REFERENCES
        // ═══════════════════════════════════════════════

        [Header("Car Selection")] [SerializeField]
        private CarStats[] carTypes;

        [Tooltip("Text showing the selected car name (replaces CarPreview image)")] [SerializeField]
        private TextMeshProUGUI carNameText;

        [Header("Weapon Selection")] [SerializeField]
        private ProjectileObject[] weaponTypes;

        [Tooltip("Text showing the selected weapon name (replaces WeaponPreview image)")] [SerializeField]
        private TextMeshProUGUI weaponNameText;

        [Header("Stats Panel")] [Tooltip("Header e.g. 'Air Bud Stats' or 'bArK-47 Stats'")] [SerializeField]
        private Text statsHeaderText;

        [Tooltip("Label next to each bar — updated dynamically")] [SerializeField]
        private Text statLabel1;

        [SerializeField] private Text statLabel2;
        [SerializeField] private Text statLabel3;
        [SerializeField] private Text statLabel4;

        [Tooltip("The four filled Image bars")] [SerializeField]
        private Image statBar1;

        [SerializeField] private Image statBar2;
        [SerializeField] private Image statBar3;
        [SerializeField] private Image statBar4;

        // ═══════════════════════════════════════════════
        //  PLAYER PREFS KEYS
        // ═══════════════════════════════════════════════

        private const string KEY_CAR = "SelectedCarTypeIndex";
        private const string KEY_WEAPON = "SelectedWeaponTypeIndex";

        // ═══════════════════════════════════════════════
        //  STATE
        // ═══════════════════════════════════════════════

        private int carIndex;
        private int weaponIndex;

        private enum StatsMode
        {
            Weapon,
            Car
        }

        private StatsMode statsMode = StatsMode.Weapon;

        // ═══════════════════════════════════════════════
        //  UNITY LIFECYCLE
        // ═══════════════════════════════════════════════

        private void Start()
        {
            carIndex = Mathf.Clamp(PlayerPrefs.GetInt(KEY_CAR, 0), 0, carTypes.Length - 1);
            weaponIndex = Mathf.Clamp(PlayerPrefs.GetInt(KEY_WEAPON, 0), 0, weaponTypes.Length - 1);

            statsMode = StatsMode.Weapon;

            RefreshCarName();
            RefreshWeaponName();
            RefreshStats();
        }

        // ═══════════════════════════════════════════════
        //  CAR NAVIGATION  (wire to bottom Left/Right buttons)
        // ═══════════════════════════════════════════════

        public void CarNext()
        {
            carIndex = (carIndex + 1) % carTypes.Length;
            statsMode = StatsMode.Car;
            RefreshCarName();
            RefreshStats();
            Save();
        }

        public void CarPrev()
        {
            carIndex = (carIndex - 1 + carTypes.Length) % carTypes.Length;
            statsMode = StatsMode.Car;
            RefreshCarName();
            RefreshStats();
            Save();
        }

        // ═══════════════════════════════════════════════
        //  WEAPON NAVIGATION  (wire to top Left/Right buttons)
        // ═══════════════════════════════════════════════

        public void WeaponNext()
        {
            weaponIndex = (weaponIndex + 1) % weaponTypes.Length;
            statsMode = StatsMode.Weapon;
            RefreshWeaponName();
            RefreshStats();
            Save();
        }

        public void WeaponPrev()
        {
            weaponIndex = (weaponIndex - 1 + weaponTypes.Length) % weaponTypes.Length;
            statsMode = StatsMode.Weapon;
            RefreshWeaponName();
            RefreshStats();
            Save();
        }

        // ═══════════════════════════════════════════════
        //  REFRESH HELPERS
        // ═══════════════════════════════════════════════

        private void RefreshCarName()
        {
            if (carNameText != null && carTypes.Length > 0)
                carNameText.text = carTypes[carIndex].CarName;
        }

        private void RefreshWeaponName()
        {
            if (weaponNameText != null && weaponTypes.Length > 0)
                weaponNameText.text = weaponTypes[weaponIndex].ProjectileName;
        }

        private void RefreshStats()
        {
            if (statsMode == StatsMode.Car)
                ApplyCarStats();
            else
                ApplyWeaponStats();
        }

        // ─── Car stats: Speed, Acceleration, Jump, Health ───────────────────────
        private void ApplyCarStats()
        {
            var s = carTypes[carIndex];

            if (statsHeaderText != null)
                statsHeaderText.text = $"{s.CarName} Stats";

            SetBar(statLabel1, statBar1, "SPD",   s.SpeedStat        / 100f);
            SetBar(statLabel2, statBar2, "ACCEL", s.AccelerationStat / 100f);
            SetBar(statLabel3, statBar3, "JUMP",  s.JumpForceStat    / 100f);
            SetBar(statLabel4, statBar4, "HP",    s.HealthStat       / 100f);
        }

        private void ApplyWeaponStats()
        {
            var w = weaponTypes[weaponIndex];

            if (statsHeaderText != null)
                statsHeaderText.text = $"{w.ProjectileName} Stats";

            float damage    = Mathf.InverseLerp(1f,    100f, w.Damage);
            float fireRate  = 1f - Mathf.InverseLerp(0.05f, 5f, w.FireRate);
            float fireForce = Mathf.InverseLerp(1f,    100f, w.FireForce);
            float recoil    = Mathf.InverseLerp(0f,    50f,  w.RecoilForce);

            SetBar(statLabel1, statBar1, "DMG",  damage);
            SetBar(statLabel2, statBar2, "RATE", fireRate);
            SetBar(statLabel3, statBar3, "SPD",  fireForce);
            SetBar(statLabel4, statBar4, "RCOL", recoil);
        }

        private void SetBar(Text label, Image bar, string labelText, float fill)
        {
            if (label != null) label.text = labelText;
            if (bar != null) bar.fillAmount = Mathf.Clamp01(fill);
        }

        // ═══════════════════════════════════════════════
        //  SAVE
        // ═══════════════════════════════════════════════

        private void Save()
        {
            PlayerPrefs.SetInt(KEY_CAR, carIndex);
            PlayerPrefs.SetInt(KEY_WEAPON, weaponIndex);
            PlayerPrefs.Save();
        }

        // ═══════════════════════════════════════════════
        //  PUBLIC GETTERS
        // ═══════════════════════════════════════════════

        public int GetSelectedCarIndex() => carIndex;
        public int GetSelectedWeaponIndex() => weaponIndex;
        public CarStats GetSelectedCarStats() => carTypes[carIndex];
        public ProjectileObject GetSelectedWeapon() => weaponTypes[weaponIndex];
    }
}