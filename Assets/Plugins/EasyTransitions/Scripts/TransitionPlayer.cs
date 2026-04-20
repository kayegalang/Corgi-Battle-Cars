using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace EasyTransition
{
    internal class TransitionPlayer : MonoBehaviour
    {
        [SerializeField] private TransitionSettings _defaultTransitionSettings;
        internal TransitionSettings DefaultTransitionSettings => _defaultTransitionSettings;
        
        [SerializeField] private Transform _transitionPanelIn;
        [SerializeField] private Transform _transitionPanelOut;
        [SerializeField] private CanvasScaler _transitionCanvas;

        private Material _multiplyColorMaterial;
        private Material _additiveColorMaterial;
        private GameObject _currentTransitionObject;
        private Tween _currentTween;

        internal void PlayIn(TransitionSettings settings, Action completion = null)
        {
            _currentTween?.Kill();
            CleanupCurrentTransition();

            _transitionCanvas.referenceResolution = settings.referenceResolution;
            _transitionPanelIn.gameObject.SetActive(true);
            _transitionPanelOut.gameObject.SetActive(false);

            _currentTransitionObject = Instantiate(settings.transitionIn, _transitionPanelIn);
            SetupTransitionObject(_currentTransitionObject, settings, settings.transitionInDuration);

            float duration = settings.transitionInDuration;

            if (completion != null)
            {
                StartCoroutine(InvokeAfterDelay(duration, completion));
            }
        }

        internal void PlayOut(TransitionSettings settings, Action completion = null)
        {
            _currentTween?.Kill();
            CleanupCurrentTransition();

            _transitionCanvas.referenceResolution = settings.referenceResolution;
            _transitionPanelIn.gameObject.SetActive(false);
            _transitionPanelOut.gameObject.SetActive(true);

            _currentTransitionObject = Instantiate(settings.transitionOut, _transitionPanelOut);
            SetupTransitionObject(_currentTransitionObject, settings, settings.transitionOutDuration);

            float duration = settings.transitionOutDuration;

            StartCoroutine(PlayOutCoroutine(duration, completion));
        }

        private IEnumerator PlayOutCoroutine(float duration, Action completion)
        {
            yield return new WaitForSeconds(duration);
            CleanupCurrentTransition();
            _transitionPanelOut.gameObject.SetActive(false);
            completion?.Invoke();
        }

        private void SetupTransitionObject(GameObject transitionObject, TransitionSettings settings, float targetDuration)
        {
            transitionObject.AddComponent<CanvasGroup>().blocksRaycasts = settings.blockRaycasts;

            _multiplyColorMaterial = settings.multiplyColorMaterial;
            _additiveColorMaterial = settings.addColorMaterial;

            if (!settings.isCutoutTransition)
            {
                ApplyColorTint(transitionObject, settings);
            }

            if (settings.flipX)
            {
                Vector3 scale = transitionObject.transform.localScale;
                transitionObject.transform.localScale = new Vector3(-scale.x, scale.y, scale.z);
            }

            if (settings.flipY)
            {
                Vector3 scale = transitionObject.transform.localScale;
                transitionObject.transform.localScale = new Vector3(scale.x, -scale.y, scale.z);
            }

            float clipLength = GetAnimatorClipLength(transitionObject);
            float speed = clipLength > 0f && targetDuration > 0f ? clipLength / targetDuration : settings.transitionSpeed;
            SetAnimatorSpeed(transitionObject, speed);
        }

        private float GetAnimatorClipLength(GameObject transitionObject)
        {
            Animator anim = transitionObject.GetComponentInChildren<Animator>();
            if (anim != null && anim.runtimeAnimatorController != null)
            {
                AnimationClip[] clips = anim.runtimeAnimatorController.animationClips;
                if (clips.Length > 0) return clips[0].length;
            }
            return 0f;
        }

        private void ApplyColorTint(GameObject transitionObject, TransitionSettings settings)
        {
            if (_multiplyColorMaterial == null || _additiveColorMaterial == null)
            {
                Debug.LogWarning("[TransitionPlayer] Color tint materials not set. Color tinting will not work.");
                return;
            }

            Material material = settings.colorTintMode == ColorTintMode.Multiply
                ? _multiplyColorMaterial
                : _additiveColorMaterial;

            if (transitionObject.TryGetComponent<Image>(out Image parentImage))
            {
                parentImage.material = material;
                parentImage.material.SetColor("_Color", settings.colorTint);
            }

            for (int i = 0; i < transitionObject.transform.childCount; i++)
            {
                if (transitionObject.transform.GetChild(i).TryGetComponent<Image>(out Image childImage))
                {
                    childImage.material = material;
                    childImage.material.SetColor("_Color", settings.colorTint);
                }
            }
        }

        private void SetAnimatorSpeed(GameObject transitionObject, float speed)
        {
            if (speed == 0) return;

            if (transitionObject.TryGetComponent<Animator>(out Animator parentAnim))
            {
                parentAnim.speed = speed;
                return;
            }

            for (int i = 0; i < transitionObject.transform.childCount; i++)
            {
                if (transitionObject.transform.GetChild(i).TryGetComponent<Animator>(out Animator childAnim))
                {
                    childAnim.speed = speed;
                }
            }
        }

        private IEnumerator InvokeAfterDelay(float duration, Action callback)
        {
            yield return new WaitForSeconds(duration);
            callback?.Invoke();
        }

        private void CleanupCurrentTransition()
        {
            if (_currentTransitionObject != null)
            {
                Destroy(_currentTransitionObject);
                _currentTransitionObject = null;
            }
        }

        private void OnDestroy()
        {
            _currentTween?.Kill();
        }
        
#if UNITY_EDITOR

        [ContextMenu("Play In Animation")]
        private void PlayInAnimation()
        {
            StartCoroutine(PlayAfterContextMenuStutter(() =>
            {
                PlayIn(_defaultTransitionSettings);
            }));
        }

        [ContextMenu("Play Out Animation")]
        private void PlayOutAnimation()
        {
            StartCoroutine(PlayAfterContextMenuStutter(() =>
            {
                PlayOut(_defaultTransitionSettings);
            }));
        }
        
        private IEnumerator PlayAfterContextMenuStutter(Action action)
        {
            yield return new WaitForSeconds(0.75f);
            action?.Invoke();
        }
#endif
    }
}
