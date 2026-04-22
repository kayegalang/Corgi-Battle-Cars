using UnityEngine;

namespace EasyTransition
{
    [CreateAssetMenu(fileName = "TransitionSettings", menuName = "Easy Transition/New Transition Settings")]
    public class TransitionSettings : ScriptableObject
    {
        [HideInInspector] public Material multiplyColorMaterial;
        [HideInInspector] public Material addColorMaterial;

        [Header("Transition Settings")]
        [Tooltip("The resolution of the canvas the transition was made in.")]
        public Vector2 referenceResolution = new Vector2(1920, 1080);

        [Tooltip("If set to true you can't interact with any UI until the transition is over.")]
        public bool blockRaycasts = true;

        [Space(10)]
        [Tooltip("Changes the color tint mode. Multiply just tints the color and Add adds the color to the transition.")]
        public ColorTintMode colorTintMode = ColorTintMode.Multiply;

        [Tooltip("Changes the color of the transition based on the color tint mode.")]
        public Color colorTint = Color.white;

        [Tooltip("If the transition uses the UICutoutMask component.")]
        public bool isCutoutTransition;

        [Space(10)]
        [Tooltip("Changes the animation speed of the transition.")]
        [Range(0.5f, 2f)]
        public float transitionSpeed = 1f;

        [Tooltip("Automatically adjust transition times based on speed.")]
        public bool autoAdjustTransitionTime = true;

        [Space(10)]
        [Tooltip("Sets the size of the transition on the x axis to -1.")]
        public bool flipX;

        [Tooltip("Sets the size of the transition on the y axis to -1.")]
        public bool flipY;

        [Space(10)]
        [Tooltip("Duration of the transition in animation.")]
        public float transitionInDuration = 1.5f;

        [Tooltip("Duration of the transition out animation.")]
        public float transitionOutDuration = 1.5f;

        [Header("Transition Prefabs")]
        [Space(10)]
        public GameObject transitionIn;
        public GameObject transitionOut;
    }

    public enum ColorTintMode
    {
        Multiply,
        Add
    }
}
