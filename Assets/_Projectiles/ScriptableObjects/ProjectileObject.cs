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
        
        [Header("Damage")]
        [Tooltip("How much damage this projectile deals on hit")]
        [SerializeField] [Range(1, 100)] private int damage = 10;
        
        [Header("Fire Rate")]
        [Tooltip("Time in seconds between shots (lower = faster shooting)")]
        [SerializeField] [Range(0.1f, 5f)] private float fireRate = 0.5f;
        
        [Header("Cooldown")]
        [Tooltip("Total cooldown time after shooting (shows on cooldown bar)")]
        [SerializeField] [Range(0.1f, 10f)] private float cooldownDuration = 2f;
        
        [Header("Recoil")]
        [Tooltip("Backward force applied to shooter's car when firing (car knockback)")]
        [SerializeField] [Range(0f, 50f)] private float recoilForce = 5f;
        
        [Header("Physics")]
        [Tooltip("Forward force applied to projectile = PROJECTILE SPEED (higher = faster projectiles)")]
        [SerializeField] [Range(1f, 100f)] private float fireForce = 30f;
        
        [Tooltip("How long the projectile exists before disappearing (seconds)")]
        [SerializeField] [Range(1f, 10f)] private float lifetime = 5f;
        
        // Visual
        public GameObject ProjectilePrefab => projectilePrefab;
        
        // Identity
        public string ProjectileName => projectileName;
        public string ProjectileDescription => projectileDescription;
        public string ProjectileID => projectileID;
        
        // Combat Stats
        public int Damage => damage;
        public float FireRate => fireRate;
        public float CooldownDuration => cooldownDuration;
        public float RecoilForce => recoilForce;
        
        // Physics
        public float FireForce => fireForce;
        public float Lifetime => lifetime;
        
        private void OnValidate()
        {
            ValidateProjectilePrefab();
            ValidateNumericStats();
        }
        
        private void ValidateProjectilePrefab()
        {
            if (projectilePrefab == null)
            {
                Debug.LogWarning($"[{nameof(ProjectileObject)}] Projectile prefab not assigned on {name}!", this);
            }
        }
        
        private void ValidateNumericStats()
        {
            if (damage <= 0) damage = 10;
            if (fireRate <= 0f) fireRate = 0.5f;
            if (cooldownDuration <= 0f) cooldownDuration = 2f;
            if (recoilForce < 0f) recoilForce = 5f;
            if (fireForce <= 0f) fireForce = 30f;
            if (lifetime <= 0f) lifetime = 5f;
        }
    }
}