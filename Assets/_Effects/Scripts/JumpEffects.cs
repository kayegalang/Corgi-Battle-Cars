using _Cars.Scripts;
using DG.Tweening;
using UnityEngine;

public class JumpEffects : MonoBehaviour
{
    [SerializeField] private CarController carController;
    [SerializeField] private SquashSettings jumpSettings = new SquashSettings()
    {
        squashAmount = -0.6f,
        duration = 0.4f,
    };
    [SerializeField] private SquashSettings landSettings = new SquashSettings()
    {
        squashAmount = 0.5f,
        duration = 0.3f,
    };
    [Tooltip("If the cars velocity is this value when landing, the squash amount will be equal to the landSettings.")]
    [SerializeField] private float landingSpeedReference = 9f;

    private Tween currentTween;

    [System.Serializable]
    private class SquashSettings
    {
        [Range(-0.8f, 0.8f)]
        public float squashAmount = 0.3f;
        public float duration = 0.3f;
        public Ease ease = Ease.OutQuad;
    }

    private void Awake()
    {
        carController.OnLand += OnLand;
        carController.OnJump += OnJump;
    }

    private void OnDestroy()
    {
        carController.OnLand -= OnLand;
        carController.OnJump -= OnJump;
    }

    private void OnLand(float landingSpeed)
    {
        print(landingSpeed);
        float t = Mathf.Clamp01(landingSpeed / landingSpeedReference);
        float squash = Mathf.Lerp(0, landSettings.squashAmount, t);

        SquashSettings scaled = new SquashSettings
        {
            squashAmount = squash,
            duration = landSettings.duration,
            ease = landSettings.ease,
        };
        PlaySquash(scaled);
    }

    private void OnJump()
    {
        PlaySquash(jumpSettings);
    }

    private void PlaySquash(SquashSettings settings)
    {
        currentTween?.Kill();

        Vector3 squashScale = new Vector3(1 + settings.squashAmount, 1 - settings.squashAmount, 1);

        transform.localScale = squashScale;
        currentTween = transform.DOScale(Vector3.one, settings.duration)
            .SetEase(settings.ease);
    }
}
