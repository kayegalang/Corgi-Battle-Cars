using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using _UI.Scripts;

/// <summary>
/// Editor tool to add ButtonSoundEffect to all buttons in the scene.
/// Access via Tools → Corgi Battle Cars → Apply Button Sound Effects
/// </summary>
public class ButtonSoundEffectApplier : EditorWindow
{
    private bool includeInactive = true;
    private bool skipExisting    = true;
    private int  lastAppliedCount = 0;

    [MenuItem("Tools/Corgi Battle Cars/Apply Button Sound Effects")]
    public static void ShowWindow()
    {
        GetWindow<ButtonSoundEffectApplier>("Button Sound Effects");
    }

    private void OnGUI()
    {
        GUILayout.Label("Button Sound Effect Applier", EditorStyles.boldLabel);
        EditorGUILayout.Space(4);

        includeInactive = EditorGUILayout.Toggle("Include Inactive Buttons", includeInactive);
        skipExisting    = EditorGUILayout.Toggle("Skip Already Applied",     skipExisting);

        EditorGUILayout.Space(8);

        // Count preview
        int count = CountButtons();
        EditorGUILayout.HelpBox($"Found {count} button(s) in scene.", MessageType.Info);

        EditorGUILayout.Space(4);

        GUI.backgroundColor = new Color(0.4f, 0.8f, 0.4f);
        if (GUILayout.Button("Apply to All Buttons in Scene", GUILayout.Height(36)))
            ApplyToAllButtons();

        GUI.backgroundColor = new Color(1f, 0.5f, 0.4f);
        if (GUILayout.Button("Remove from All Buttons in Scene", GUILayout.Height(28)))
            RemoveFromAllButtons();

        GUI.backgroundColor = Color.white;

        if (lastAppliedCount > 0)
        {
            EditorGUILayout.Space(8);
            EditorGUILayout.HelpBox($"✓ Applied to {lastAppliedCount} button(s)!", MessageType.Info);
        }
    }

    private int CountButtons()
    {
        var buttons = FindObjectsByType<Button>(
            includeInactive ? FindObjectsInactive.Include : FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);
        return buttons.Length;
    }

    private void ApplyToAllButtons()
    {
        var buttons = FindObjectsByType<Button>(
            includeInactive ? FindObjectsInactive.Include : FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);

        int applied = 0;

        foreach (var button in buttons)
        {
            if (skipExisting && button.GetComponent<ButtonSoundEffect>() != null)
                continue;

            Undo.RecordObject(button.gameObject, "Add ButtonSoundEffect");
            button.gameObject.AddComponent<ButtonSoundEffect>();
            EditorUtility.SetDirty(button.gameObject);
            applied++;
        }

        lastAppliedCount = applied;
        Debug.Log($"[ButtonSoundEffectApplier] Added ButtonSoundEffect to {applied} button(s).");
    }

    private void RemoveFromAllButtons()
    {
        var effects = FindObjectsByType<ButtonSoundEffect>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (var effect in effects)
        {
            Undo.RecordObject(effect.gameObject, "Remove ButtonSoundEffect");
            DestroyImmediate(effect);
        }

        lastAppliedCount = 0;
        Debug.Log($"[ButtonSoundEffectApplier] Removed ButtonSoundEffect from {effects.Length} button(s).");
    }
}
