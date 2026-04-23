using UnityEngine;

namespace _Cars.ScriptableObjects
{
    [CreateAssetMenu(fileName = "CarStats", menuName = "Scriptable Objects/CarStats")]
    public class CarStats : ScriptableObject
    {
        [Header("Car Identity")]
        [Tooltip("Display name of the car")]
        [SerializeField] private string carName = "Unnamed Car";
        
        [Tooltip("Description shown in UI")]
        [TextArea(2, 4)]
        [SerializeField] private string carDescription = "A racing car.";
        
        [Tooltip("Unique identifier for this car type")]
        [SerializeField] private string carID = "default_car";

        [Header("Car Visuals")]
        [Tooltip("The car body mesh prefab to instantiate at CarModelMount")]
        [SerializeField] private GameObject carModelPrefab;

        [Tooltip("Local position offset of the car model relative to CarModelMount")]
        [SerializeField] private Vector3 carModelOffset = Vector3.zero;

        [Tooltip("Local rotation offset of the car model")]
        [SerializeField] private Vector3 carModelRotation = Vector3.zero;

        [Tooltip("Local scale of the car model — set to match the prefab's original scale e.g. (500, 500, 500)")]
        [SerializeField] private Vector3 carModelScale    = Vector3.one;

        [Tooltip("Per-weapon offsets — one entry per weapon, same order as CharacterSelectUI")]
        [SerializeField] private WeaponMountOffset[] weaponOffsets = new WeaponMountOffset[0];

        [System.Serializable]
        public class WeaponMountOffset
        {
            [Tooltip("Label only — helps identify which weapon this is for")]
            public string weaponName = "Weapon";
            public Vector3 positionOffset = Vector3.zero;
            public Vector3 rotationOffset = Vector3.zero;
            public Vector3 scaleOffset    = Vector3.one;
        }

        // Visual accessors
        public GameObject      CarModelPrefab        => carModelPrefab;
        public Vector3         CarModelOffset        => carModelOffset;
        public Vector3         CarModelRotationEuler => carModelRotation;
        public Quaternion      CarModelRotation      => Quaternion.Euler(carModelRotation);
        public Vector3         CarModelScale         => carModelScale;
        public WeaponMountOffset[] WeaponOffsets     => weaponOffsets;

        public Vector3    GetWeaponPositionOffset(int weaponIndex) =>
            weaponIndex < weaponOffsets.Length ? weaponOffsets[weaponIndex].positionOffset : Vector3.zero;

        public Quaternion GetWeaponRotationOffset(int weaponIndex) =>
            weaponIndex < weaponOffsets.Length
                ? Quaternion.Euler(weaponOffsets[weaponIndex].rotationOffset)
                : Quaternion.identity;

        public Vector3    GetWeaponScaleOffset(int weaponIndex) =>
            weaponIndex < weaponOffsets.Length ? weaponOffsets[weaponIndex].scaleOffset : Vector3.one;
        
        [Header("Car Stats (0-100%)")]
        [SerializeField] [Range(0f, 100f)] private float speedStat        = 50f;
        [SerializeField] [Range(0f, 100f)] private float accelerationStat = 50f;
        [SerializeField] [Range(0f, 100f)] private float jumpForceStat    = 50f;
        [SerializeField] [Range(0f, 100f)] private float healthStat       = 50f;
        
        [Header("Speed Range")]
        [SerializeField] private float minMaxSpeed = 25f;
        [SerializeField] private float maxMaxSpeed = 50f;
        
        [Header("Acceleration Range")]
        [SerializeField] private float minAcceleration = 12f;
        [SerializeField] private float maxAcceleration = 30f;
        
        [Header("Jump Force Range")]
        [SerializeField] private float minJumpForce = 40f;
        [SerializeField] private float maxJumpForce = 50f;
        
        [Header("Health Range")]
        [SerializeField] private int minMaxHealth = 75;
        [SerializeField] private int maxMaxHealth = 125;
        
        [Header("Turn Speed Range")]
        [SerializeField] private float minTurnSpeed = 20f;
        [SerializeField] private float maxTurnSpeed = 35f;
        
        [Header("Ground Detection")]
        [SerializeField] private Vector3 groundCheckOffset   = new Vector3(0f, 0.26f, 0f);
        [SerializeField] [Range(0.1f, 2f)] private float groundCheckDistance = 0.26f;
        
        // Runtime properties
        public float   Acceleration      => Mathf.Lerp(minAcceleration, maxAcceleration, accelerationStat / 100f);
        public float   TurnSpeed         => Mathf.Lerp(minTurnSpeed,    maxTurnSpeed,    speedStat        / 100f);
        public float   MaxSpeed          => Mathf.Lerp(minMaxSpeed,     maxMaxSpeed,     speedStat        / 100f);
        public float   JumpForce         => Mathf.Lerp(minJumpForce,    maxJumpForce,    jumpForceStat    / 100f);
        public int     MaxHealth         => Mathf.RoundToInt(Mathf.Lerp(minMaxHealth, maxMaxHealth, healthStat / 100f));
        public Vector3 GroundCheckOffset => groundCheckOffset;
        public float   GroundCheckDistance => groundCheckDistance;
        
        // Identity
        public string CarName        => carName;
        public string CarDescription => carDescription;
        public string CarID          => carID;
        
        // UI stats
        public float SpeedStat        => speedStat;
        public float AccelerationStat => accelerationStat;
        public float JumpForceStat    => jumpForceStat;
        public float HealthStat       => healthStat;
        
        private void OnValidate()
        {
            if (minMaxSpeed        >= maxMaxSpeed)        Debug.LogWarning($"[CarStats] Min max speed must be less than max on {name}!");
            if (minAcceleration    >= maxAcceleration)    Debug.LogWarning($"[CarStats] Min acceleration must be less than max on {name}!");
            if (minJumpForce       >= maxJumpForce)       Debug.LogWarning($"[CarStats] Min jump force must be less than max on {name}!");
            if (minMaxHealth       >= maxMaxHealth)       Debug.LogWarning($"[CarStats] Min health must be less than max on {name}!");
            if (minTurnSpeed       >= maxTurnSpeed)       Debug.LogWarning($"[CarStats] Min turn speed must be less than max on {name}!");
        }
    }
}