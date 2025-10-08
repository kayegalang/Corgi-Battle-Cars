using UnityEngine;

namespace Player.Scripts
{
    [CreateAssetMenu(fileName = "CarStats", menuName = "Scriptable Objects/CarStats")]
    public class CarStats : ScriptableObject
    {
        public float acceleration = 20f;
        public float turnSpeed = 20f;
        public Vector3 groundCheckOffset = new Vector3(0f, 0.26f, 0f);
        public float groundCheckDistance = 0.26f;
        public float jumpForce = 5f;

    }
}

