using System;
using System.Collections;
using EasyTransition;
using UnityEngine;

public class DebugScript : MonoBehaviour
{
    [SerializeField] private TransitionSettings transition;

    [ContextMenu("Play Transition In")]
    public void PlayTransitionIn()
    {
        StartCoroutine(InvokeAfterTime(0.2f, () =>
        {
            TransitionManager.PlayTransitionIn(transition);
        }));
    }

    [ContextMenu("Play Transition Out")]
    public void PlayTransitionOut()
    {
        StartCoroutine(InvokeAfterTime(0.2f, () =>
        {
            TransitionManager.PlayTransitionOut(transition);
        }));
    }

    [ContextMenu("Play Full Transition")]
    public void PlayFullTransition()
    {
        StartCoroutine(InvokeAfterTime(0.2f, () =>
        {
            // Disable input
            TransitionManager.PlayTransitionIn(transition, completion: () =>
            {
                // Re-enable input
                TransitionManager.PlayTransitionOut(transition);
            });
        }));
    }

    private IEnumerator InvokeAfterTime(float delay, Action completion)
    {
        yield return new WaitForSecondsRealtime(delay);
        completion?.Invoke();
    }
}
