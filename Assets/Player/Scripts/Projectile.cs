using System;
using System.Collections;
using Unity.Cinemachine;
using Unity.VisualScripting;
using UnityEngine;

namespace Player.Scripts
{
    public class Projectile : MonoBehaviour
    {
        private GameObject shooter;
        private bool canDoDamage = false;
        void Start()
        {
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

            if (!other.CompareTag(shooter.tag))
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
                health.TakeDamage(10);
            }
        }


        public void SetShooter(GameObject shooterObject)
        {
            shooter = shooterObject;
        }
    }
}

