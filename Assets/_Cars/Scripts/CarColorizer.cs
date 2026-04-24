using UnityEngine;

namespace _Cars.Scripts
{
    /// <summary>
    /// Sets the hue shift on the car's material based on player index
    /// so duplicate car selections are visually distinct.
    /// 
    /// Called by CarVisualLoader.ResolveReferences() after the car model spawns.
    /// 
    /// Requires the car material to use a Shader Graph with a promoted
    /// float parameter (default name: "HueShift", range 0-360).
    /// </summary>
    public class CarColorizer : MonoBehaviour
    {
        [Header("Shader Property")]
        [Tooltip("Must match the promoted parameter name in Shader Graph exactly")]
        [SerializeField] private string hueShiftPropertyName = "HueShift";

        [Header("Hue Shift Values Per Player")]
        [Tooltip("Index 0 = P1, 1 = P2, 2 = P3, 3 = P4")]
        [SerializeField] private float[] playerHues = { 0f, 0f, 120f, 200f };
        //                                              P1   P2   P3    P4
        //                                             Red  Blue Green Yellow

        private static readonly System.Collections.Generic.Dictionary<string, int> TagToIndex
            = new System.Collections.Generic.Dictionary<string, int>
        {
            { "PlayerOne",   0 },
            { "PlayerTwo",   1 },
            { "PlayerThree", 2 },
            { "PlayerFour",  3 },
        };

        // ═══════════════════════════════════════════════
        //  PUBLIC API — called by CarVisualLoader
        // ═══════════════════════════════════════════════

        /// <summary>
        /// Called by CarVisualLoader after spawning — passes the player index
        /// since the car model prefab doesn't have the player tag.
        /// Also called by CharacterSelectPreview for preview cars.
        /// </summary>
        public void ApplyColor(int playerIdx)
        {
            if (playerIdx < 0 || playerIdx >= playerHues.Length) return;

            float      hue       = playerHues[playerIdx];
            Renderer[] renderers = GetComponentsInChildren<Renderer>();

            ApplyHueShift(renderers, hue);
            Debug.Log($"[CarColorizer] P{playerIdx + 1} → hue {hue}°");
        }

        /// <summary>Called by CarVisualLoader passing pre-gathered renderers.</summary>
        public void ApplyColor(Renderer[] renderers)
        {
            int playerIdx = GetPlayerIndex();
            if (playerIdx < 0 || playerIdx >= playerHues.Length) return;

            float hue = playerHues[playerIdx];
            ApplyHueShift(renderers, hue);
            Debug.Log($"[CarColorizer] {gameObject.tag} → hue {hue}°");
        }

        // ═══════════════════════════════════════════════
        //  INTERNAL
        // ═══════════════════════════════════════════════

        private void ApplyHueShift(Renderer[] renderers, float hue)
        {
            Debug.Log($"[CarColorizer] Applying hue {hue} to {renderers.Length} renderers on {gameObject.name}");

            foreach (var r in renderers)
            {
                if (r == null) continue;
                Debug.Log($"[CarColorizer] Renderer: {r.name}, materials: {r.materials.Length}");

                foreach (var mat in r.materials)
                {
                    if (mat == null) continue;
                    bool hasProperty = mat.HasProperty(hueShiftPropertyName);
                    Debug.Log($"[CarColorizer] Material: {mat.name}, has '{hueShiftPropertyName}': {hasProperty}");

                    if (hasProperty)
                    {
                        mat.SetFloat(hueShiftPropertyName, hue);
                        Debug.Log($"[CarColorizer] ✓ Set {hueShiftPropertyName} = {hue} on {mat.name}");
                    }
                }
            }
        }

        private int GetPlayerIndex()
        {
            if (TagToIndex.TryGetValue(gameObject.tag, out int index))
                return index;
            return -1;
        }
    }
}