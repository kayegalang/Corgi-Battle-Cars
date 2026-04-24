using System.Collections.Generic;
using UnityEngine;
using FMOD.Studio;
using FMODUnity;
using _Audio.scripts;

namespace _PowerUps.Scripts
{
    /// <summary>
    /// Draws animated smell trails from the power-up toward each player OR bot in range.
    /// Creates multiple parallel wavy lines per entity for a thick ribbon effect.
    /// Add to the same GameObject as PowerUpPickup.
    /// </summary>
    public class SmellTrailRenderer : MonoBehaviour
    {
        [Header("Trail Settings")]
        [Tooltip("How far the smell trail reaches toward players and bots")]
        [SerializeField] private float smellDistance = 25f;

        [Tooltip("Width of each individual line")]
        [SerializeField] private float trailWidth = 0.12f;

        [Tooltip("How fast the trail texture scrolls toward the player")]
        [SerializeField] private float scrollSpeed = 1.5f;

        [Tooltip("How much the trail waves side to side")]
        [SerializeField] private float waveAmplitude = 0.25f;

        [Tooltip("How fast the trail waves")]
        [SerializeField] private float waveFrequency = 2f;

        [Tooltip("Number of segments in each line (more = smoother wave)")]
        [SerializeField] private int segments = 20;

        [Tooltip("Color of the trail")]
        [SerializeField] private Color trailColor = new Color(1f, 0.85f, 0.1f, 0.6f);

        [Tooltip("The material to use for the trail — use Particles/Additive")]
        [SerializeField] private Material trailMaterial;

        [Tooltip("Vertical offset so the trail floats above the ground")]
        [SerializeField] private float heightOffset = 0.5f;

        [Header("Multi-Line Settings")]
        [Tooltip("How many parallel lines to draw per trail")]
        [SerializeField] private int lineCount = 4;

        [Tooltip("How far apart the parallel lines are")]
        [SerializeField] private float lineSeparation = 0.07f;

        [Tooltip("Phase offset per line — makes lines braid around each other")]
        [SerializeField] private float linePhaseOffset = 0.5f;

        private readonly Dictionary<GameObject, List<LineRenderer>> activeTrails =
            new Dictionary<GameObject, List<LineRenderer>>();

        // Single sniff sound — plays when ANY player is in range
        private EventInstance sniffInstance;
        private bool          sniffPlaying = false;

        private static readonly string[] AllTags =
        {
            "PlayerOne", "PlayerTwo", "PlayerThree", "PlayerFour",
            "BotOne",    "BotTwo",    "BotThree",    "BotFour"
        };

        private float textureOffset = 0f;

        // ═══════════════════════════════════════════════
        //  UNITY LIFECYCLE
        // ═══════════════════════════════════════════════

        private void Update()
        {
            textureOffset -= scrollSpeed * Time.deltaTime;
            UpdateTrails();
            UpdateSniffSoundPositions();
        }

        private void UpdateSniffSoundPositions()
        {
            if (sniffPlaying)
                sniffInstance.set3DAttributes(RuntimeUtils.To3DAttributes(transform));
        }

        private void OnDestroy()
        {
            RemoveAllTrails();
        }

        // ═══════════════════════════════════════════════
        //  TRAIL UPDATE
        // ═══════════════════════════════════════════════

        private void UpdateTrails()
        {
            List<GameObject> entitiesInRange = GetEntitiesInRange();

            foreach (GameObject entity in entitiesInRange)
                if (!activeTrails.ContainsKey(entity))
                    AddTrail(entity);

            List<GameObject> toRemove = new List<GameObject>();
            foreach (var kvp in activeTrails)
                if (!entitiesInRange.Contains(kvp.Key) || kvp.Key == null)
                    toRemove.Add(kvp.Key);

            foreach (GameObject entity in toRemove)
                RemoveTrail(entity);

            foreach (var kvp in activeTrails)
                if (kvp.Key != null && kvp.Value != null)
                    UpdateTrailPositions(kvp.Value, kvp.Key);
        }

        // ═══════════════════════════════════════════════
        //  ENTITY DETECTION — players AND bots
        // ═══════════════════════════════════════════════

        private List<GameObject> GetEntitiesInRange()
        {
            var entities = new List<GameObject>();

            foreach (string tag in AllTags)
            {
                try
                {
                    GameObject[] tagged = GameObject.FindGameObjectsWithTag(tag);
                    foreach (GameObject entity in tagged)
                    {
                        if (entity == null) continue;

                        float dist = Vector3.Distance(transform.position, entity.transform.position);
                        if (dist <= smellDistance)
                            entities.Add(entity);
                    }
                }
                catch { }
            }

            return entities;
        }

        // ═══════════════════════════════════════════════
        //  TRAIL MANAGEMENT
        // ═══════════════════════════════════════════════

        private void AddTrail(GameObject entity)
        {
            var lines = new List<LineRenderer>();

            for (int i = 0; i < lineCount; i++)
            {
                GameObject trailGO = new GameObject($"SmellTrail_{entity.name}_Line{i}");
                trailGO.transform.SetParent(transform);

                LineRenderer lr  = trailGO.AddComponent<LineRenderer>();
                lr.positionCount = segments + 1;
                lr.startWidth    = trailWidth;
                lr.endWidth      = trailWidth * 0.4f;
                lr.useWorldSpace = true;
                lr.textureMode   = LineTextureMode.Tile;

                if (trailMaterial != null)
                    lr.material = new Material(trailMaterial);
                else
                    lr.material = new Material(Shader.Find("Particles/Additive"));

                lr.material.color = trailColor;

                float alphaScale = 1f - (Mathf.Abs(i - (lineCount - 1) / 2f) / lineCount) * 0.35f;

                Gradient gradient = new Gradient();
                gradient.SetKeys(
                    new GradientColorKey[]
                    {
                        new GradientColorKey(trailColor, 0f),
                        new GradientColorKey(trailColor, 1f)
                    },
                    new GradientAlphaKey[]
                    {
                        new GradientAlphaKey(0.85f * alphaScale, 0f),
                        new GradientAlphaKey(0f, 1f)
                    }
                );
                lr.colorGradient = gradient;

                lines.Add(lr);
            }

            activeTrails[entity] = lines;

            // Start sniff sound if not already playing
            if (!sniffPlaying)
                StartSniffSound();
        }

        private void RemoveTrail(GameObject entity)
        {
            if (activeTrails.TryGetValue(entity, out List<LineRenderer> lines))
            {
                foreach (var lr in lines)
                    if (lr != null) Destroy(lr.gameObject);

                activeTrails.Remove(entity);
            }

            // Stop sniff sound only when no players are in range
            if (activeTrails.Count == 0)
                StopSniffSound();
        }

        private void RemoveAllTrails()
        {
            foreach (var kvp in activeTrails)
                foreach (var lr in kvp.Value)
                    if (lr != null) Destroy(lr.gameObject);

            activeTrails.Clear();
            StopSniffSound();
        }

        // ═══════════════════════════════════════════════
        //  SNIFF SOUND
        // ═══════════════════════════════════════════════

        private void StartSniffSound()
        {
            if (FMODEvents.instance == null || sniffPlaying) return;

            sniffInstance = RuntimeManager.CreateInstance(FMODEvents.instance.sniff);
            sniffInstance.set3DAttributes(RuntimeUtils.To3DAttributes(transform));
            sniffInstance.start();
            sniffPlaying = true;
        }

        private void StopSniffSound()
        {
            if (!sniffPlaying) return;
            sniffInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
            sniffInstance.release();
            sniffPlaying = false;
        }

        // ═══════════════════════════════════════════════
        //  TRAIL POSITIONS
        // ═══════════════════════════════════════════════

        private void UpdateTrailPositions(List<LineRenderer> lines, GameObject entity)
        {
            Vector3 start = transform.position + Vector3.up * heightOffset;
            Vector3 end   = entity.transform.position + Vector3.up * heightOffset;

            Vector3 direction     = (end - start).normalized;
            Vector3 perpendicular = Vector3.Cross(direction, Vector3.up).normalized;

            for (int lineIdx = 0; lineIdx < lines.Count; lineIdx++)
            {
                LineRenderer lr = lines[lineIdx];
                if (lr == null) continue;

                float lateralOffset = (lineIdx - (lines.Count - 1) / 2f) * lineSeparation;
                float phase         = lineIdx * linePhaseOffset;

                for (int i = 0; i <= segments; i++)
                {
                    float   t        = (float)i / segments;
                    Vector3 straight = Vector3.Lerp(start, end, t);

                    float wave = Mathf.Sin(t * Mathf.PI * 3f + Time.time * waveFrequency + phase)
                                 * waveAmplitude
                                 * Mathf.Sin(t * Mathf.PI);

                    lr.SetPosition(i, straight + perpendicular * (wave + lateralOffset));
                }

                lr.material.mainTextureOffset = new Vector2(textureOffset, 0f);
            }
        }

        // ═══════════════════════════════════════════════
        //  GIZMOS
        // ═══════════════════════════════════════════════

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.85f, 0.1f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, smellDistance);
        }
    }
}