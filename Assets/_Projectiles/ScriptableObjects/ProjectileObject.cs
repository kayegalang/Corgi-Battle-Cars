using UnityEngine;

namespace _Projectiles.ScriptableObjects
{
    [CreateAssetMenu(fileName = "Projectile", menuName = "Scriptable Objects/Projectile")]
    public class ProjectileObject : ScriptableObject
    {
        [Header("Projectile Identity")]
        [Tooltip("Display name of the projectile/weapon (e.g., 'Plasma Cannon', 'Machine Gun')")]
        [SerializeField] private string projectileName = "Unnamed Weapon";
        
        [Tooltip("Description shown in UI (e.g., 'High damage but slow fire rate')")]
        [TextArea(2, 4)]
        [SerializeField] private string projectileDescription = "A projectile weapon.";
        
        [Tooltip("Unique identifier for this projectile type (e.g., 'plasma_cannon', 'machine_gun')")]
        [SerializeField] private string projectileID = "default_projectile";
        
        [Header("Visual")]
        [Tooltip("The prefab instantiated when firing")]
        [SerializeField] private GameObject projectilePrefab;

        // ── Damage ──────────────────────────────────────
        [Header("Damage")]
        [Tooltip("Designer-facing 0–100 stat. Interpolates between Min and Max Damage.")]
        [SerializeField] [Range(0f, 100f)] private float damageStat = 50f;

        [Tooltip("Damage when Damage Stat = 0")]
        [SerializeField] [Range(1, 200)] private int minDamage = 5;

        [Tooltip("Damage when Damage Stat = 100")]
        [SerializeField] [Range(1, 200)] private int maxDamage = 50;

        // ── Fire Rate ────────────────────────────────────
        [Header("Fire Rate")]
        [Tooltip("Designer-facing 0–100 stat. Higher = faster shooting. Interpolates between Min and Max Fire Rate.")]
        [SerializeField] [Range(0f, 100f)] private float fireRateStat = 50f;

        [Tooltip("Slowest fire rate (seconds between shots) — used when Fire Rate Stat = 0")]
        [SerializeField] [Range(0.05f, 5f)] private float maxFireRate = 2f;

        [Tooltip("Fastest fire rate (seconds between shots) — used when Fire Rate Stat = 100")]
        [SerializeField] [Range(0.05f, 5f)] private float minFireRate = 0.1f;

        // ── Cooldown ─────────────────────────────────────
        [Header("Cooldown")]
        [Tooltip("Designer-facing 0–100 stat. Higher = faster cooldown. Interpolates between Min and Max.")]
        [SerializeField] [Range(0f, 100f)] private float cooldownStat = 50f;

        [Tooltip("Slowest cooldown duration — used when Cooldown Stat = 0")]
        [SerializeField] [Range(0.1f, 20f)] private float maxCooldownDuration = 8f;

        [Tooltip("Fastest cooldown duration — used when Cooldown Stat = 100")]
        [SerializeField] [Range(0.1f, 20f)] private float minCooldownDuration = 1f;

        // ── Recoil ───────────────────────────────────────
        [Header("Recoil")]
        [Tooltip("Designer-facing 0–100 stat. Interpolates between Min and Max Recoil.")]
        [SerializeField] [Range(0f, 100f)] private float recoilStat = 50f;

        [Tooltip("Recoil force when Recoil Stat = 0")]
        [SerializeField] [Range(0f, 100f)] private float minRecoilForce = 0f;

        [Tooltip("Recoil force when Recoil Stat = 100")]
        [SerializeField] [Range(0f, 100f)] private float maxRecoilForce = 30f;

        // ── Fire Force (fixed, not designer-tunable) ─────
        [Header("Physics — Fixed Values")]
        [Tooltip("Forward force applied to the projectile. Not designer-tunable — change here directly.")]
        [SerializeField] [Range(1f, 100f)] private float fireForce = 30f;

        [Tooltip("How long the projectile exists before disappearing (seconds)")]
        [SerializeField] [Range(1f, 10f)] private float lifetime = 5f;

        // ─────────────────────────────────────────────────
        //  IDENTITY
        // ─────────────────────────────────────────────────
        public string ProjectileName        => projectileName;
        public string ProjectileDescription => projectileDescription;
        public string ProjectileID          => projectileID;

        // ─────────────────────────────────────────────────
        //  VISUAL
        // ─────────────────────────────────────────────────
        public GameObject ProjectilePrefab => projectilePrefab;

        // ─────────────────────────────────────────────────
        //  STAT PERCENTAGES (used by TuningManager sliders)
        // ─────────────────────────────────────────────────
        public float DamageStat   => damageStat;
        public float FireRateStat => fireRateStat;
        public float CooldownStat => cooldownStat;
        public float RecoilStat   => recoilStat;

        // ─────────────────────────────────────────────────
        //  COMPUTED RUNTIME VALUES (used by CarShooter)
        // ─────────────────────────────────────────────────

        /// <summary>Damage interpolated from stat percentage</summary>
        public int Damage =>
            Mathf.RoundToInt(Mathf.Lerp(minDamage, maxDamage, damageStat / 100f));

        /// <summary>Seconds between shots — higher stat = lower value = faster fire</summary>
        public float FireRate =>
            Mathf.Lerp(maxFireRate, minFireRate, fireRateStat / 100f);

        /// <summary>Cooldown duration — higher stat = lower value = faster cooldown</summary>
        public float CooldownDuration =>
            Mathf.Lerp(maxCooldownDuration, minCooldownDuration, cooldownStat / 100f);

        /// <summary>Recoil force interpolated from stat percentage</summary>
        public float RecoilForce =>
            Mathf.Lerp(minRecoilForce, maxRecoilForce, recoilStat / 100f);

        /// <summary>Fixed — not designer-tunable via sliders</summary>
        public float FireForce => fireForce;

        /// <summary>Fixed — not designer-tunable via sliders</summary>
        public float Lifetime => lifetime;

        // ─────────────────────────────────────────────────
        //  MIN / MAX ACCESSORS (used by TuningManager ranges)
        // ─────────────────────────────────────────────────
        public int   MinDamage          => minDamage;
        public int   MaxDamage          => maxDamage;
        public float MinFireRate        => minFireRate;
        public float MaxFireRate        => maxFireRate;
        public float MinCooldownDuration => minCooldownDuration;
        public float MaxCooldownDuration => maxCooldownDuration;
        public float MinRecoilForce     => minRecoilForce;
        public float MaxRecoilForce     => maxRecoilForce;

        // ─────────────────────────────────────────────────
        //  VALIDATION
        // ─────────────────────────────────────────────────
        private void OnValidate()
        {
            if (projectilePrefab == null)
                Debug.LogWarning($"[{nameof(ProjectileObject)}] Projectile prefab not assigned on {name}!", this);

            // Clamp ranges so min never exceeds max
            minDamage           = Mathf.Min(minDamage,            maxDamage);
            minFireRate         = Mathf.Min(minFireRate,          maxFireRate);
            minCooldownDuration = Mathf.Min(minCooldownDuration,  maxCooldownDuration);
            minRecoilForce      = Mathf.Min(minRecoilForce,       maxRecoilForce);

            if (fireForce <= 0f)  fireForce  = 30f;
            if (lifetime  <= 0f)  lifetime   = 5f;
        }
    }
}