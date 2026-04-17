using _PowerUps.Scripts;
using UnityEngine;

namespace _PowerUps.ScriptableObjects
{
    public abstract class PowerUpObject : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Display name shown in UI")]
        public string powerUpName = "Power Up";
        
        [Tooltip("Short description shown in UI")]
        [TextArea(1, 3)]
        public string description = "";
        
        [Tooltip("Icon shown in UI when collected")]
        public Sprite icon;
        
        [Tooltip("Type used to identify this power-up")]
        public PowerUpType type;
        
        [Header("Duration")]
        [Tooltip("How long the power-up lasts in seconds. Set to 0 for instant effects.")]
        [Range(0f, 30f)]
        public float duration = 5f;
        
        [Header("Pickup Settings")]
        [Tooltip("Distance at which the treat becomes visible")]
        [Range(1f, 20f)]
        public float revealDistance = 5f;
        
        [Tooltip("Distance at which the controller starts vibrating")]
        [Range(1f, 50f)]
        public float vibrationDistance = 15f;
        
        [Tooltip("Distance at which smell trails start appearing")]
        [Range(1f, 50f)]
        public float smellDistance = 25f;
        
        [Header("Visuals")]
        [Tooltip("Color of the smell trail particles for this power-up")]
        public Color smellTrailColor = Color.yellow;
        
        [Tooltip("Particle system prefab used for the smell trail")]
        public GameObject smellTrailPrefab;
        
        [Tooltip("The treat mesh/visual shown when the power-up is revealed")]
        public GameObject treatVisualPrefab;
        
        [Header("Audio")]
        [Tooltip("Sound played when the power-up is collected")]
        public AudioClip collectSound;
        
        [Tooltip("Sound played while the power-up is active")]
        public AudioClip activeSound;
        
        /// <summary>
        /// Called the moment the player collects this power-up.
        /// Override this to implement the effect.
        /// </summary>
        public abstract void Apply(GameObject player);
        
        /// <summary>
        /// Called when the power-up duration ends (or instantly if duration is 0).
        /// Override this to clean up the effect.
        /// </summary>
        public abstract void Remove(GameObject player);
        
        /// <summary>
        /// Optional: Called every frame while the power-up is active.
        /// Override this for effects that need per-frame updates (e.g. poop trail).
        /// </summary>
        public virtual void OnUpdate(GameObject player) { }
    }
}