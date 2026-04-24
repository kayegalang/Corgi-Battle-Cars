using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace _UI.Scripts
{
    public class CooldownBarUI : MonoBehaviour
    {
        [Header("Cooldown Sprites")]
        [SerializeField] private Sprite[] cooldownSprites;

        [Header("UI References")]
        [SerializeField] private Image cooldownImage;

        [Header("Flash Overlay")]
        [SerializeField] private GameObject backgroundFlash;

        [Header("Flash Settings")]
        [SerializeField] private float flashInterval = 0.3f;

        private float currentCooldown;

        // Overheat state
        private bool isOverheated;
        private Coroutine pulseRoutine;

        private void Start()
        {
            ValidateComponents();

            SetCooldown(0, 1);

            if (backgroundFlash != null)
                backgroundFlash.SetActive(false);
        }

        // =========================
        // COOLANT DISPLAY
        // =========================
        public void UpdateCooldown(float current, float max)
        {
            currentCooldown = current;

            if (cooldownImage == null || cooldownSprites == null || cooldownSprites.Length == 0)
                return;

            float percent = Mathf.Clamp01(current / max);

            int index = Mathf.RoundToInt(percent * (cooldownSprites.Length - 1));
            index = Mathf.Clamp(index, 0, cooldownSprites.Length - 1);

            cooldownImage.sprite = cooldownSprites[index];
        }

        public void SetCooldown(float current, float max)
        {
            UpdateCooldown(current, max);
        }

        // =========================
        // OVERHEAT CONTROL (IMPORTANT)
        // =========================
        public void SetOverheatState(bool state)
        {
            isOverheated = state;

            if (isOverheated)
                StartPulse();
            else
                StopPulse();
        }

        // =========================
        // PULSE SYSTEM
        // =========================
        private void StartPulse()
        {
            if (backgroundFlash == null) return;

            if (pulseRoutine != null)
                StopCoroutine(pulseRoutine);

            pulseRoutine = StartCoroutine(PulseLoop());
        }

        private void StopPulse()
        {
            if (pulseRoutine != null)
            {
                StopCoroutine(pulseRoutine);
                pulseRoutine = null;
            }

            if (backgroundFlash != null)
                backgroundFlash.SetActive(false);
        }

        private IEnumerator PulseLoop()
        {
            while (isOverheated)
            {
                backgroundFlash.SetActive(true);
                yield return new WaitForSeconds(flashInterval);

                backgroundFlash.SetActive(false);
                yield return new WaitForSeconds(flashInterval);
            }
        }

        // =========================
        // SAFETY
        // =========================
        private void ValidateComponents()
        {
            if (cooldownImage == null)
                Debug.LogError($"[{nameof(CooldownBarUI)}] cooldownImage not assigned!");

            if (backgroundFlash == null)
                Debug.LogWarning($"[{nameof(CooldownBarUI)}] backgroundFlash not assigned!");
        }
    }
}