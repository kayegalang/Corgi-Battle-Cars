using UnityEngine;

namespace _Projectiles.ScriptableObjects
{
    [CreateAssetMenu(fileName = "Projectile", menuName = "Scriptable Objects/Projectile")]
    public class ProjectileObject : ScriptableObject
    {
        [Header("Projectile Identity")]
        [SerializeField] private string projectileName        = "Unnamed Weapon";
        [TextArea(2, 4)]
        [SerializeField] private string projectileDescription = "A projectile weapon.";
        [SerializeField] private string projectileID          = "default_projectile";

        [Header("Weapon Visuals")]
        [Tooltip("The weapon mesh prefab to instantiate at WeaponMount")]
        [SerializeField] private GameObject weaponModelPrefab;

        [Tooltip("Local position offset of the weapon model relative to WeaponMount")]
        [SerializeField] private Vector3 weaponModelOffset = Vector3.zero;

        [Tooltip("Local rotation offset of the weapon model")]
        [SerializeField] private Vector3 weaponModelRotation = Vector3.zero;
        
        [Header("Audio")]
        [Tooltip("Sound played when this weapon fires")]
        [SerializeField] private FMODUnity.EventReference fireSound;
        public FMODUnity.EventReference FireSound => fireSound;

        // Weapon visual accessors
        public GameObject WeaponModelPrefab   => weaponModelPrefab;
        public Vector3    WeaponModelOffset   => weaponModelOffset;
        public Quaternion WeaponModelRotation => Quaternion.Euler(weaponModelRotation);

        [Header("Visual")]
        [Tooltip("The prefab instantiated when firing")]
        [SerializeField] private GameObject projectilePrefab;

        [Header("Laser Settings")]
        [Tooltip("If true, damage is dealt via raycast in the weapon visual — no projectile is spawned")]
        [SerializeField] private bool isLaser = false;
        public bool IsLaser => isLaser;

        [Header("Damage")]
        [SerializeField] [Range(0f, 100f)] private float damageStat  = 50f;
        [SerializeField] [Range(1, 200)]   private int   minDamage    = 5;
        [SerializeField] [Range(1, 200)]   private int   maxDamage    = 50;

        [Header("Fire Rate")]
        [SerializeField] [Range(0f, 100f)] private float fireRateStat = 50f;
        [SerializeField] [Range(0.05f, 5f)] private float maxFireRate = 2f;
        [SerializeField] [Range(0.05f, 5f)] private float minFireRate = 0.1f;

        [Header("Cooldown")]
        [SerializeField] [Range(0f, 100f)] private float cooldownStat        = 50f;
        [SerializeField] [Range(0.1f, 20f)] private float maxCooldownDuration = 8f;
        [SerializeField] [Range(0.1f, 20f)] private float minCooldownDuration = 1f;

        [Header("Recoil")]
        [SerializeField] [Range(0f, 100f)] private float recoilStat     = 50f;
        [SerializeField] [Range(0f, 100f)] private float minRecoilForce = 0f;
        [SerializeField] [Range(0f, 100f)] private float maxRecoilForce = 30f;

        [Header("Physics — Fixed Values")]
        [SerializeField] [Range(1f, 100f)] private float fireForce = 30f;
        [SerializeField] [Range(1f, 10f)]  private float lifetime  = 5f;

        // Identity
        public string ProjectileName        => projectileName;
        public string ProjectileDescription => projectileDescription;
        public string ProjectileID          => projectileID;

        // Visual
        public GameObject ProjectilePrefab => projectilePrefab;

        // Stat percentages
        public float DamageStat   => damageStat;
        public float FireRateStat => fireRateStat;
        public float CooldownStat => cooldownStat;
        public float RecoilStat   => recoilStat;

        // Computed runtime values
        public int   Damage           => Mathf.RoundToInt(Mathf.Lerp(minDamage, maxDamage, damageStat / 100f));
        public float FireRate         => Mathf.Lerp(maxFireRate, minFireRate, fireRateStat / 100f);
        public float CooldownDuration => Mathf.Lerp(maxCooldownDuration, minCooldownDuration, cooldownStat / 100f);
        public float RecoilForce      => Mathf.Lerp(minRecoilForce, maxRecoilForce, recoilStat / 100f);
        public float FireForce        => fireForce;
        public float Lifetime         => lifetime;

        // Min/max accessors for TuningManager
        public int   MinDamage           => minDamage;
        public int   MaxDamage           => maxDamage;
        public float MinFireRate         => minFireRate;
        public float MaxFireRate         => maxFireRate;
        public float MinCooldownDuration => minCooldownDuration;
        public float MaxCooldownDuration => maxCooldownDuration;
        public float MinRecoilForce      => minRecoilForce;
        public float MaxRecoilForce      => maxRecoilForce;

        private void OnValidate()
        {
            if (projectilePrefab == null)
                Debug.LogWarning($"[ProjectileObject] Projectile prefab not assigned on {name}!");

            minDamage           = Mathf.Min(minDamage,           maxDamage);
            minFireRate         = Mathf.Min(minFireRate,         maxFireRate);
            minCooldownDuration = Mathf.Min(minCooldownDuration, maxCooldownDuration);
            minRecoilForce      = Mathf.Min(minRecoilForce,      maxRecoilForce);

            if (fireForce <= 0f) fireForce = 30f;
            if (lifetime  <= 0f) lifetime  = 5f;
        }
    }
}