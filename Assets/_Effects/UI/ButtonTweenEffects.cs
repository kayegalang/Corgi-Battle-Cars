using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;

public class ButtonTweenEffects : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler,
    IPointerClickHandler,
    ISelectHandler, IDeselectHandler
{
    [Header("Navigation Settings")]
    [Tooltip("Prevents selection with the pointer, as we want to reserve selection for gamepads.")]
    [SerializeField] private bool deselectOnPointerClick = true;
    
    [Header("Scale Settings")]
    [SerializeField] private float hoverScale = 1.1f;
    [SerializeField] private float scaleDuration = 0.15f;

    [Header("Click Jiggle Settings")]
    [SerializeField] private float jiggleStrength = 15f;
    [SerializeField] private float jiggleDuration = 0.4f;
    [SerializeField] private float jiggleFirstStepSpeedMultiplier = 1f;
    [SerializeField] private float shakePunchIntensity = 0.1f;

    private RectTransform rect;
    private Vector3 originalScale;
    private Quaternion originalRotation;

    private Tween scaleTween;
    private Tween rotateTween;
    private Tween punchTween;

    private bool scaledUp;
    private bool isHovered;
    private bool isSelected;

    private void Awake()
    {
        rect = GetComponent<RectTransform>();
        originalScale = rect.localScale;
        originalRotation = rect.localRotation;
    }
    
    #region Input Events

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;
        RefreshScale();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
        RefreshScale();
    }
    
    public void OnSelect(BaseEventData eventData)
    {
        isSelected = true;
        RefreshScale();
    }

    public void OnDeselect(BaseEventData eventData)
    {
        isSelected = false;
        RefreshScale();
    }
    
    public void OnPointerClick(PointerEventData eventData)
    {
        Jiggle();
        if (deselectOnPointerClick) eventData.selectedObject = null;
    }

    public void OnSubmit(BaseEventData eventData)
    {
        Jiggle();
    }

    private void OnDisable()
    {
        isHovered = false;
        isSelected = false;
    }

    #endregion

    #region Tween Effects
    
    private void RefreshScale()
    {
        bool wasScaledUp = scaledUp;
        scaledUp = isHovered || isSelected;
        
        print($"{wasScaledUp} was: {scaledUp}");
        
        if (scaledUp == wasScaledUp) return;
        
        if (scaledUp)
        {
            ScaleUp();
        }
        else
        {
            ScaleDown();
        }
    }
    
    private void ScaleUp()
    {
        scaleTween?.Kill();
        scaleTween = rect.DOScale(originalScale * hoverScale, scaleDuration)
            .SetEase(Ease.OutQuad);
    }

    private void ScaleDown()
    {
        scaleTween?.Kill();
        scaleTween = rect.DOScale(originalScale, scaleDuration)
            .SetEase(Ease.OutQuad);
    }
    
    private void Jiggle()
    {
        rotateTween?.Kill();

        rect.localRotation = originalRotation;

        float stepDuration = jiggleDuration / 3f;
        float firstStepDuration = stepDuration / jiggleFirstStepSpeedMultiplier;

        Sequence seq = DOTween.Sequence();
        seq.Append(rect.DOLocalRotate(new Vector3(0, 0, -jiggleStrength), firstStepDuration).SetEase(Ease.InOutSine));
        seq.Append(rect.DOLocalRotate(new Vector3(0, 0,  jiggleStrength), stepDuration).SetEase(Ease.InOutSine));
        seq.Append(rect.DOLocalRotate(Vector3.zero, stepDuration).SetEase(Ease.OutSine));
        seq.OnComplete(() => rect.localRotation = originalRotation);

        rotateTween = seq;

        punchTween?.Kill(true);
        punchTween = rect.DOPunchScale(Vector3.one * shakePunchIntensity, 0.2f, 8, 0.5f);
    }
    
    #endregion
}