using System.Collections.Generic;
using UnityEngine;

namespace _PowerUps.Scripts
{
    /// <summary>
    /// Draws animated smell trails from the power-up toward each player in range.
    /// Uses LineRenderers with a scrolling material so it looks like particles flowing
    /// toward the player. Add to the same GameObject as PowerUpPickup.
    /// </summary>
    public class SmellTrailRenderer : MonoBehaviour
    {
        [Header("Trail Settings")]
        [Tooltip("How far the smell trail reaches toward players")]
        [SerializeField] private float smellDistance = 25f;

        [Tooltip("Width of the trail line")]
        [SerializeField] private float trailWidth = 0.15f;

        [Tooltip("How fast the trail texture scrolls toward the player")]
        [SerializeField] private float scrollSpeed = 1.5f;

        [Tooltip("How much the trail waves side to side")]
        [SerializeField] private float waveAmplitude = 0.3f;

        [Tooltip("How fast the trail waves")]
        [SerializeField] private float waveFrequency = 2f;

        [Tooltip("Number of segments in the trail (more = smoother wave)")]
        [SerializeField] private int segments = 20;

        [Tooltip("Color of the trail — should match your power-up smell color")]
        [SerializeField] private Color trailColor = new Color(1f, 0.85f, 0.1f, 0.6f);

        [Tooltip("The material to use for the trail — use Particles/Additive")]
        [SerializeField] private Material trailMaterial;

        [Tooltip("Vertical offset so the trail floats above the ground")]
        [SerializeField] private float heightOffset = 0.5f;

        // One LineRenderer per player currently in smell range
        private readonly Dictionary<GameObject, LineRenderer> activeTrails =
            new Dictionary<GameObject, LineRenderer>();

        private readonly string[] playerTags =
            { "PlayerOne", "PlayerTwo", "PlayerThree", "PlayerFour" };

        private float textureOffset = 0f;

        // ═══════════════════════════════════════════════
        //  UNITY LIFECYCLE
        // ═══════════════════════════════════════════════

        private void Update()
        {
            // Scroll the texture offset to animate the trail
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

            // Add trails for newly in-range players
            foreach (GameObject player in playersInRange)
            {
                if (!activeTrails.ContainsKey(player))
                    AddTrail(player);
            }

            // Remove trails for out-of-range players
            List<GameObject> toRemove = new List<GameObject>();
            foreach (var kvp in activeTrails)
            {
                if (!playersInRange.Contains(kvp.Key) || kvp.Key == null)
                    toRemove.Add(kvp.Key);
            }
            foreach (GameObject player in toRemove)
                RemoveTrail(player);

            // Update positions of active trails
            foreach (var kvp in activeTrails)
            {
                if (kvp.Key != null && kvp.Value != null)
                    UpdateTrailPositions(kvp.Value, kvp.Key);
            }
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
            GameObject trailGO = new GameObject($"SmellTrail_{player.name}");
            trailGO.transform.SetParent(transform);

            LineRenderer lr = trailGO.AddComponent<LineRenderer>();
            lr.positionCount  = segments + 1;
            lr.startWidth     = trailWidth;
            lr.endWidth       = trailWidth * 0.5f;
            lr.useWorldSpace  = true;
            lr.textureMode    = LineTextureMode.Tile;

            // Use assigned material or fall back to a new one
            if (trailMaterial != null)
            {
                lr.material = new Material(trailMaterial);
            }
            else
            {
                // Fallback: create a simple transparent material
                lr.material = new Material(Shader.Find("Particles/Additive"));
            }

            lr.material.color = trailColor;

            // Set gradient — fades out near the player end
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[]
                {
                    new GradientColorKey(trailColor, 0f),
                    new GradientColorKey(trailColor, 1f)
                },
                new GradientAlphaKey[]
                {
                    new GradientAlphaKey(0.8f, 0f),  // bright at power-up
                    new GradientAlphaKey(0f,   1f)   // fades out at player end
                }
            );
            lr.colorGradient = gradient;

            activeTrails[player] = lr;

            Debug.Log($"[SmellTrailRenderer] Added trail toward {player.name}");
        }

        private void RemoveTrail(GameObject player)
        {
            if (activeTrails.TryGetValue(player, out LineRenderer lr))
            {
                if (lr != null)
                    Destroy(lr.gameObject);

                activeTrails.Remove(player);
                Debug.Log($"[SmellTrailRenderer] Removed trail toward {(player != null ? player.name : "destroyed player")}");
            }
        }

        private void RemoveAllTrails()
        {
            foreach (var kvp in activeTrails)
                if (kvp.Value != null) Destroy(kvp.Value.gameObject);

            activeTrails.Clear();
        }

        // ═══════════════════════════════════════════════
        //  TRAIL POSITIONS
        // ═══════════════════════════════════════════════

        private void UpdateTrailPositions(LineRenderer lr, GameObject player)
        {
            Vector3 start = transform.position + Vector3.up * heightOffset;
            Vector3 end   = player.transform.position + Vector3.up * heightOffset;

            // Direction perpendicular to the trail for waving
            Vector3 direction    = (end - start).normalized;
            Vector3 perpendicular = Vector3.Cross(direction, Vector3.up).normalized;

            for (int i = 0; i <= segments; i++)
            {
                float   t          = (float)i / segments;
                Vector3 straight   = Vector3.Lerp(start, end, t);

                // Sine wave offset — fades out near start and end
                float   wave       = Mathf.Sin(t * Mathf.PI * 3f + Time.time * waveFrequency)
                                     * waveAmplitude
                                     * Mathf.Sin(t * Mathf.PI); // envelope: 0 at ends, peak in middle

                Vector3 point      = straight + perpendicular * wave;
                lr.SetPosition(i, point);
            }

            // Scroll texture to animate flow toward player
            lr.material.mainTextureOffset = new Vector2(textureOffset, 0f);
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