using UnityEngine;

namespace _Gameplay.Scripts
{
    /// <summary>
    /// Smooth follow camera that tracks a bot from behind and above.
    /// Snaps instantly into position for the first few frames so the
    /// camera starts in the right place before the countdown begins.
    /// Used by FakeMultiplayerDirector.
    /// </summary>
    public class BotFollowCamera : MonoBehaviour
    {
        [Header("Target")]
        public Transform target;

        [Header("Follow Settings")]
        [SerializeField] private float followDistance  = 8f;
        [SerializeField] private float followHeight    = 4f;
        [SerializeField] private float followSmooth    = 5f;
        [SerializeField] private float lookAheadOffset = 1f;

        [Header("Snap Settings")]
        [Tooltip("Number of frames to snap instantly before switching to smooth follow")]
        [SerializeField] private int snapFrames = 5;

        private int remainingSnapFrames;

        // ═══════════════════════════════════════════════
        //  LIFECYCLE
        // ═══════════════════════════════════════════════

        private void Awake()
        {
            remainingSnapFrames = snapFrames;
        }

        private void LateUpdate()
        {
            if (target == null) return;

            Vector3 desiredPos = target.position
                - target.forward * followDistance
                + Vector3.up     * followHeight;

            if (remainingSnapFrames > 0)
            {
                // Snap instantly for the first N frames
                transform.position  = desiredPos;
                remainingSnapFrames--;
            }
            else
            {
                // Then switch to smooth follow
                transform.position = Vector3.Lerp(
                    transform.position,
                    desiredPos,
                    followSmooth * Time.deltaTime);
            }

            Vector3 lookTarget = target.position + target.forward * lookAheadOffset;
            transform.LookAt(lookTarget);
        }

        // ═══════════════════════════════════════════════
        //  PUBLIC
        // ═══════════════════════════════════════════════

        /// <summary>
        /// Forces the camera to snap to its target immediately.
        /// Called by FakeMultiplayerDirector before the countdown starts.
        /// </summary>
        public void SnapToTarget()
        {
            if (target == null) return;

            Vector3 desiredPos  = target.position
                - target.forward * followDistance
                + Vector3.up     * followHeight;

            transform.position  = desiredPos;

            Vector3 lookTarget  = target.position + target.forward * lookAheadOffset;
            transform.LookAt(lookTarget);

            remainingSnapFrames = 0; // switch to smooth follow immediately after
        }
    }
}