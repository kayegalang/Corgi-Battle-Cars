using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace _PowerUps.Scripts
{
    /// <summary>
    /// Spawns expanding glowing rings under the car while Super Jump is active
    /// and the car is moving upward. Uses LineRenderers to draw solid circles
    /// that grow and fade out. Add to the player prefab root.
    /// </summary>
    public class SuperJumpRingEffect : MonoBehaviour
    {
        [Header("Ring Settings")]
        [Tooltip("How often a new ring spawns while active and moving up")]
        [SerializeField] private float spawnInterval = 0.3f;

        [Tooltip("How big the ring grows before fading out")]
        [SerializeField] private float maxRadius = 3f;

        [Tooltip("Starting radius of each ring")]
        [SerializeField] private float startRadius = 0.3f;

        [Tooltip("How long each ring takes to expand and fade")]
        [SerializeField] private float ringDuration = 0.6f;

        [Tooltip("How far below the car the rings appear")]
        [SerializeField] private float heightOffset = -0.5f;

        [Tooltip("Width of the ring line")]
        [SerializeField] private float lineWidth = 0.12f;

        [Tooltip("How many points make up the circle — more = smoother")]
        [SerializeField] private int circlePoints = 32;

        [Tooltip("Minimum upward velocity required to spawn a ring")]
        [SerializeField] private float minUpwardVelocity = 0.5f;

        [Header("Appearance")]
        [Tooltip("Color of the ring")]
        [SerializeField] private Color ringColor = new Color(0.4f, 0.8f, 1f, 1f);

        [Tooltip("Material for the ring — use Particles/Additive")]
        [SerializeField] private Material ringMaterial;

        private bool             isActive = false;
        private Coroutine        spawnCoroutine;
        private List<GameObject> activeRings = new List<GameObject>();
        private Rigidbody        rb;

        // ═══════════════════════════════════════════════
        //  LIFECYCLE
        // ═══════════════════════════════════════════════

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
        }

        private void OnDestroy()
        {
            Deactivate();
        }

        // ═══════════════════════════════════════════════
        //  PUBLIC API
        // ═══════════════════════════════════════════════

        public void Activate()
        {
            isActive       = true;
            spawnCoroutine = StartCoroutine(SpawnRingsLoop());
            Debug.Log("[SuperJumpRingEffect] Activated!");
        }

        public void Deactivate()
        {
            isActive = false;
            if (spawnCoroutine != null)
            {
                StopCoroutine(spawnCoroutine);
                spawnCoroutine = null;
            }
            Debug.Log("[SuperJumpRingEffect] Deactivated!");
        }

        // ═══════════════════════════════════════════════
        //  SPAWN LOOP
        // ═══════════════════════════════════════════════

        private IEnumerator SpawnRingsLoop()
        {
            while (isActive)
            {
                // Only spawn rings when car is moving upward
                if (rb != null && rb.linearVelocity.y > minUpwardVelocity)
                    SpawnRing();

                yield return new WaitForSeconds(spawnInterval);
            }
        }

        private void SpawnRing()
        {
            GameObject ringGO = new GameObject("SuperJumpRing");
            ringGO.transform.position = transform.position + Vector3.up * heightOffset;
            ringGO.transform.rotation = Quaternion.identity;

            // Set to same layer as player camera so it renders correctly
            Camera playerCamera = GetComponentInChildren<Camera>();
            if (playerCamera != null)
                ringGO.layer = playerCamera.gameObject.layer;

            LineRenderer lr  = ringGO.AddComponent<LineRenderer>();
            lr.positionCount = circlePoints + 1;
            lr.startWidth    = lineWidth;
            lr.endWidth      = lineWidth;
            lr.useWorldSpace = true;
            lr.loop          = false;

            if (ringMaterial != null)
                lr.material = new Material(ringMaterial);
            else
                lr.material = new Material(Shader.Find("Particles/Additive"));

            lr.material.color = ringColor;

            activeRings.Add(ringGO);

            StartCoroutine(AnimateRing(ringGO, lr));
        }

        // ═══════════════════════════════════════════════
        //  RING ANIMATION
        // ═══════════════════════════════════════════════

        private IEnumerator AnimateRing(GameObject ringGO, LineRenderer lr)
        {
            float elapsed = 0f;

            while (elapsed < ringDuration)
            {
                if (ringGO == null) yield break;

                float t      = elapsed / ringDuration;
                float radius = Mathf.Lerp(startRadius, maxRadius, t);
                float alpha  = Mathf.Lerp(1f, 0f, t);

                DrawCircle(lr, ringGO.transform.position, radius);

                Color c = ringColor;
                c.a           = alpha;
                lr.startColor = c;
                lr.endColor   = c;

                elapsed += Time.deltaTime;
                yield return null;
            }

            activeRings.Remove(ringGO);
            Destroy(ringGO);
        }

        private void DrawCircle(LineRenderer lr, Vector3 center, float radius)
        {
            for (int i = 0; i <= circlePoints; i++)
            {
                float angle = (float)i / circlePoints * Mathf.PI * 2f;
                float x     = Mathf.Cos(angle) * radius;
                float z     = Mathf.Sin(angle) * radius;

                lr.SetPosition(i, center + new Vector3(x, 0f, z));
            }
        }
    }
}