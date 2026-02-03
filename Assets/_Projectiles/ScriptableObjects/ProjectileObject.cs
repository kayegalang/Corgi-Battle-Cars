using UnityEngine;

namespace _Projectiles.ScriptableObjects
{
    [CreateAssetMenu(fileName = "Projectile", menuName = "Scriptable Objects/Projectile")]
    public class ProjectileObject : ScriptableObject
    {
        [Header("Projectile Configuration")]
        [Tooltip("The prefab that will be instantiated when firing")]
        [SerializeField] private GameObject projectilePrefab;
        
        [Tooltip("The force applied to the projectile when fired")]
        [SerializeField] [Range(1f, 100f)] private float fireForce = 30f;
        
        [Tooltip("Time in seconds between shots (lower = faster shooting)")]
        [SerializeField] [Range(0.1f, 5f)] private float fireRate = 0.25f;
        
        public GameObject ProjectilePrefab => projectilePrefab;
        public float FireForce => fireForce;
        public float FireRate => fireRate;
        
        private void OnValidate()
        {
            ValidateProjectilePrefab();
            ValidateFireForce();
            ValidateFireRate();
        }
        
        private void ValidateProjectilePrefab()
        {
            if (projectilePrefab == null)
            {
                Debug.LogWarning($"[{nameof(ProjectileObject)}] Projectile prefab is not assigned on {name}!", this);
            }
        }
        
        private void ValidateFireForce()
        {
            if (fireForce <= 0f)
            {
                Debug.LogWarning($"[{nameof(ProjectileObject)}] Fire force should be greater than 0 on {name}!", this);
                fireForce = 30f;
            }
        }
        
        private void ValidateFireRate()
        {
            if (fireRate <= 0f)
            {
                Debug.LogWarning($"[{nameof(ProjectileObject)}] Fire rate should be greater than 0 on {name}!", this);
                fireRate = 0.25f;
            }
        }
    }
}