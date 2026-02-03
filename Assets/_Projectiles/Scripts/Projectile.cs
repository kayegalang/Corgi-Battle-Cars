using System.Collections;
using _Cars.Scripts;
using UnityEngine;

namespace _Projectiles.Scripts
{
    public class Projectile : MonoBehaviour
    {
        [Header("Projectile Settings")]
        [SerializeField] private float lifetime = 5f;
        [SerializeField] private float damageDelay = 0.01f;
        [SerializeField] private int damageAmount = 10;
        
        private GameObject shooter;
        private bool canDoDamage = false;
        
        private void Start()
        {
            ScheduleDestruction();
            StartCoroutine(EnableDamageAfterDelay());
        }
        
        private void ScheduleDestruction()
        {
            Destroy(gameObject, lifetime);
        }
        
        private IEnumerator EnableDamageAfterDelay()
        {
            yield return new WaitForSeconds(damageDelay);
            canDoDamage = true;
        }
        
        private void OnTriggerEnter(Collider other)
        {
            if (!CanDamageTarget(other))
            {
                return;
            }
            
            DamageTarget(other);
            DestroyProjectile();
        }
        
        private bool CanDamageTarget(Collider other)
        {
            if (!canDoDamage)
            {
                return false;
            }
            
            if (IsShooter(other))
            {
                return false;
            }
            
            return true;
        }
        
        private bool IsShooter(Collider other)
        {
            if (shooter == null)
            {
                return false;
            }
            
            return other.CompareTag(shooter.tag);
        }
        
        private void DamageTarget(Collider other)
        {
            CarHealth health = other.GetComponent<CarHealth>();
            
            if (health != null)
            {
                health.TakeDamage(damageAmount, shooter);
            }
        }
        
        private void DestroyProjectile()
        {
            Destroy(gameObject);
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
        
        public GameObject GetShooter()
        {
            return shooter;
        }
    }
}