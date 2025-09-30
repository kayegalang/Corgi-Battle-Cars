using System;
using System.Collections;
using Unity.Cinemachine;
using Unity.VisualScripting;
using UnityEngine;

namespace Gameplay.Scripts
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
            yield return new WaitForSeconds(0.1f);
            canDoDamage = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!canDoDamage) return;

            if (!other.CompareTag(shooter.tag))
            {
                Destroy(gameObject);
            }
        }
        
        
        public void SetShooter(GameObject shooterObject)
        {
            shooter = shooterObject;
        }
    }
}

