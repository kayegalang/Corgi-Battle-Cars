using System.Collections;
using _Cars.Scripts;
using UnityEngine;

namespace _Projectiles.Scripts
{
    public class Projectile : MonoBehaviour
    {
        [Header("Configuration")]
        [Tooltip("Damage amount (set by ProjectileObject)")]
        [SerializeField] private int damageAmount = 10;
        
        [Tooltip("How long projectile exists before auto-destroying")]
        [SerializeField] private float lifetime = 5f;
        
        [Tooltip("Small delay before projectile can damage (prevents self-damage)")]
        [SerializeField] private float damageDelay = 0.01f;
        
        private GameObject shooter;
        private bool canDealDamage;
        
        private void Start()
        {
            ScheduleAutoDestruction();
            StartCoroutine(EnableDamageAfterShortDelay());
        }
        
        private void ScheduleAutoDestruction()
        {
            Destroy(gameObject, lifetime);
        }
        
        private IEnumerator EnableDamageAfterShortDelay()
        {
            yield return new WaitForSeconds(damageDelay);
            canDealDamage = true;
        }
        
        private void OnTriggerEnter(Collider other)
        {
            if (ShouldIgnoreCollision(other))
            {
                return;
            }
            
            ApplyDamageToTarget(other);
            DestroyThisProjectile();
        }
        
        private bool ShouldIgnoreCollision(Collider other)
        {
            if (!CanDealDamageYet())
            {
                return true;
            }
            
            if (HitOwnShooter(other))
            {
                return true;
            }
            
            return false;
        }
        
        private bool CanDealDamageYet()
        {
            return canDealDamage;
        }
        
        private bool HitOwnShooter(Collider other)
        {
            if (shooter == null)
            {
                return false;
            }
            
            return other.CompareTag(shooter.tag);
        }
        
        private void ApplyDamageToTarget(Collider other)
        {
            CarHealth targetHealth = other.GetComponent<CarHealth>();
            
            if (targetHealth != null)
            {
                targetHealth.TakeDamage(damageAmount, shooter);
            }
        }
        
        private void DestroyThisProjectile()
        {
            Destroy(gameObject);
        }
        
        public void ConfigureProjectile(GameObject shooterObject, int damage, float projectileLifetime)
        {
            SetShooter(shooterObject);
            SetDamage(damage);
            SetLifetime(projectileLifetime);
        }
        
        public void SetShooter(GameObject shooterObject)
        {
            if (shooterObject == null)
            {
                Debug.LogWarning($"[{nameof(Projectile)}] Trying to set null shooter!");
                return;
            }
            
            shooter = shooterObject;
        }
        
        public void SetDamage(int damage)
        {
            damageAmount = damage;
        }
        
        public void SetLifetime(float projectileLifetime)
        {
            lifetime = projectileLifetime;
        }
        
        public GameObject GetShooter()
        {
            return shooter;
        }
    }
}