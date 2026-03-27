using System.Collections.Generic;
using UnityEngine;

namespace _PowerUps.Scripts
{
    /// <summary>
    /// Draws animated smell trails from the power-up toward each player in range.
    /// Creates multiple parallel wavy lines per player for a thick ribbon effect.
    /// Each trail is on the target player's layer so only their camera sees it.
    /// Add to the same GameObject as PowerUpPickup.
    /// </summary>
    public class SmellTrailRenderer : MonoBehaviour
    {
        [Header("Trail Settings")]
        [Tooltip("How far the smell trail reaches toward players")]
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

        // List of LineRenderers per player
        private readonly Dictionary<GameObject, List<LineRenderer>> activeTrails =
            new Dictionary<GameObject, List<LineRenderer>>();

        private readonly string[] playerTags =
            { "PlayerOne", "PlayerTwo", "PlayerThree", "PlayerFour" };

        private float textureOffset = 0f;

        // ═══════════════════════════════════════════════
        //  UNITY LIFECYCLE
        // ═══════════════════════════════════════════════

        private void Update()
        {
            textureOffset -= scrollSpeed * Time.deltaTime;
            UpdateTrails();
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
            List<GameObject> playersInRange = GetPlayersInRange();

            foreach (GameObject player in playersInRange)
                if (!activeTrails.ContainsKey(player))
                    AddTrail(player);

            List<GameObject> toRemove = new List<GameObject>();
            foreach (var kvp in activeTrails)
                if (!playersInRange.Contains(kvp.Key) || kvp.Key == null)
                    toRemove.Add(kvp.Key);

            foreach (GameObject player in toRemove)
                RemoveTrail(player);

            foreach (var kvp in activeTrails)
                if (kvp.Key != null && kvp.Value != null)
                    UpdateTrailPositions(kvp.Value, kvp.Key);
        }

        // ═══════════════════════════════════════════════
        //  PLAYER DETECTION
        // ═══════════════════════════════════════════════

        private List<GameObject> GetPlayersInRange()
        {
            var players = new List<GameObject>();

            foreach (string tag in playerTags)
            {
                try
                {
                    GameObject[] tagged = GameObject.FindGameObjectsWithTag(tag);
                    foreach (GameObject player in tagged)
                    {
                        if (player == null) continue;

                        // Skip bots — no PlayerInput means it's a bot
                        if (player.GetComponent<UnityEngine.InputSystem.PlayerInput>() == null)
                            continue;

                        float dist = Vector3.Distance(transform.position, player.transform.position);
                        if (dist <= smellDistance)
                            players.Add(player);
                    }
                }
                catch { }
            }

            return players;
        }

        // ═══════════════════════════════════════════════
        //  TRAIL MANAGEMENT
        // ═══════════════════════════════════════════════

        private void AddTrail(GameObject player)
        {
            // Get player's layer so only their camera renders this trail
            Camera playerCamera = player.GetComponentInChildren<Camera>();
            int    playerLayer  = playerCamera != null ? playerCamera.gameObject.layer : 0;

            var lines = new List<LineRenderer>();

            for (int i = 0; i < lineCount; i++)
            {
                GameObject trailGO = new GameObject($"SmellTrail_{player.name}_Line{i}");
                trailGO.transform.SetParent(transform);
                trailGO.layer = playerLayer;

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

                // Centre lines are brighter, outer lines fade slightly
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

            activeTrails[player] = lines;
            Debug.Log($"[SmellTrailRenderer] Added {lineCount}-line trail toward {player.name}");
        }

        private void RemoveTrail(GameObject player)
        {
            if (activeTrails.TryGetValue(player, out List<LineRenderer> lines))
            {
                foreach (var lr in lines)
                    if (lr != null) Destroy(lr.gameObject);

                activeTrails.Remove(player);
            }
        }

        private void RemoveAllTrails()
        {
            foreach (var kvp in activeTrails)
                foreach (var lr in kvp.Value)
                    if (lr != null) Destroy(lr.gameObject);

            activeTrails.Clear();
        }

        // ═══════════════════════════════════════════════
        //  TRAIL POSITIONS
        // ═══════════════════════════════════════════════

        private void UpdateTrailPositions(List<LineRenderer> lines, GameObject player)
        {
            Vector3 start = transform.position + Vector3.up * heightOffset;
            Vector3 end   = player.transform.position + Vector3.up * heightOffset;

            Vector3 direction     = (end - start).normalized;
            Vector3 perpendicular = Vector3.Cross(direction, Vector3.up).normalized;

            for (int lineIdx = 0; lineIdx < lines.Count; lineIdx++)
            {
                LineRenderer lr = lines[lineIdx];
                if (lr == null) continue;

                // Each line is offset laterally — spread evenly around centre
                float lateralOffset = (lineIdx - (lines.Count - 1) / 2f) * lineSeparation;

                // Each line has a different wave phase so they braid nicely
                float phase = lineIdx * linePhaseOffset;

                for (int i = 0; i <= segments; i++)
                {
                    float   t        = (float)i / segments;
                    Vector3 straight = Vector3.Lerp(start, end, t);

                    float wave = Mathf.Sin(t * Mathf.PI * 3f + Time.time * waveFrequency + phase)
                                 * waveAmplitude
                                 * Mathf.Sin(t * Mathf.PI); // envelope: 0 at ends, peak in middle

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