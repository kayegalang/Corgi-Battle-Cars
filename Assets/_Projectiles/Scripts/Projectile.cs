using System.Collections;
using _Cars.Scripts;
using UnityEngine;

namespace _Projectiles.Scripts
{
    public class Projectile : MonoBehaviour
    {
        public static GameObject Shooter;
        private bool canDoDamage = false;
        void Start()
        { ;
            Destroy(gameObject, 5f);
            StartCoroutine(DoDamage());
        }

        private IEnumerator DoDamage()
        {
            yield return new WaitForSeconds(0.01f);
            canDoDamage = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!canDoDamage) return;

            if (!other.CompareTag(Shooter.tag))
            {
                DamageEnemy(other);
                
                Destroy(gameObject);
            }
        }

        private static void DamageEnemy(Collider other)
        {
            CarHealth health = other.gameObject.GetComponent<CarHealth>();

            if (health != null)
            {
                health.TakeDamage(10, Shooter);
            }
        }
        
        public void SetShooter(GameObject shooterObject)
        {
            Shooter = shooterObject;
        }

        public GameObject GetShooter()
        {
            return Shooter;
        }
        
    }
}

