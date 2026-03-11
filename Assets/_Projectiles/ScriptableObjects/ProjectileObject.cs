using UnityEngine;

namespace _Projectiles.ScriptableObjects
{
    [CreateAssetMenu(fileName = "Projectile", menuName = "Scriptable Objects/Projectile")]
    public class ProjectileObject : ScriptableObject
    {
        [Header("Visual")]
        [Tooltip("The prefab instantiated when firing")]
        [SerializeField] private GameObject projectilePrefab;
        
        [Header("Damage")]
        [Tooltip("How much damage this projectile deals")]
        [SerializeField] [Range(1, 100)] private int damage = 10;
        
        [Header("Fire Rate")]
        [Tooltip("Time in seconds between shots (lower = faster shooting)")]
        [SerializeField] [Range(0.1f, 5f)] private float fireRate = 0.5f;
        
        [Header("Cooldown")]
        [Tooltip("Total cooldown time after shooting (shows on cooldown bar)")]
        [SerializeField] [Range(0.1f, 10f)] private float cooldownDuration = 2f;
        
        [Header("Recoil")]
        [Tooltip("Backward force applied to shooter when firing")]
        [SerializeField] [Range(0f, 50f)] private float recoilForce = 5f;
        
        [Header("Physics")]
        [Tooltip("Forward force applied to projectile when fired")]
        [SerializeField] [Range(1f, 100f)] private float fireForce = 30f;
        
        [Tooltip("How long the projectile exists before auto-destroying")]
        [SerializeField] [Range(1f, 10f)] private float lifetime = 5f;
        
        public GameObject ProjectilePrefab => projectilePrefab;
        public int Damage => damage;
        public float FireRate => fireRate;
        public float CooldownDuration => cooldownDuration;
        public float RecoilForce => recoilForce;
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