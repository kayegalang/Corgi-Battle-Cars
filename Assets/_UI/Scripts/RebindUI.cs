using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using UnityEngine.UI;

public class RebindUI : MonoBehaviour
{
    private PlayerInput playerInput;

    [Header("Setup")]
    public string actionName;
    public string bindingGroup; // "Keyboard" or "Controller"

    [Header("UI")]
    public TMP_Text bindingDisplayText;
    public Button rebindButton;

    [Header("Binding")]
    public int bindingIndex = 0;

    public void SetPlayer(PlayerInput input)
    {
        playerInput = input;
        UpdateBindingDisplay();
    }

    void Start()
    {
        if (rebindButton != null)
            rebindButton.onClick.AddListener(StartRebind);
        else
            Debug.LogError($"{name}: Rebind Button not assigned");
    }

    void UpdateBindingDisplay()
    {
        if (playerInput == null || bindingDisplayText == null)
            return;

        var action = playerInput.actions[actionName];

        if (action == null || bindingIndex < 0 || bindingIndex >= action.bindings.Count)
        {
            bindingDisplayText.text = $"Invalid Binding";
            return;
        }

        var binding = action.bindings[bindingIndex];

        bindingDisplayText.text =
            InputControlPath.ToHumanReadableString(
                binding.effectivePath,
                InputControlPath.HumanReadableStringOptions.OmitDevice
            );
    }

    void StartRebind()
    {
        if (playerInput == null)
        {
            Debug.LogError($"{name}: PlayerInput is NULL");
            return;
        }

        var action = playerInput.actions[actionName];

        if (action == null)
        {
            Debug.LogError($"{name}: Action '{actionName}' not found");
            return;
        }

        if (bindingIndex < 0 || bindingIndex >= action.bindings.Count)
        {
            Debug.LogError($"{name}: Binding index out of range");
            return;
        }

        rebindButton.interactable = false;

        action.Disable();

        bindingDisplayText.text = $"Press new {bindingGroup} input...";

        var rebind = action.PerformInteractiveRebinding(bindingIndex);

        // 🔑 Proper filtering
        if (bindingGroup == "Keyboard")
        {
            rebind.WithControlsExcluding("<Gamepad>");
        }
        else if (bindingGroup == "Controller")
        {
            rebind.WithControlsHavingToMatchPath("<Gamepad>");
        }

        rebind.OnComplete(operation =>
        {
            operation.Dispose();
            action.Enable();

            UpdateBindingDisplay();
            rebindButton.interactable = true;
        });

        rebind.OnCancel(operation =>
        {
            operation.Dispose();
            action.Enable();

            UpdateBindingDisplay();
            rebindButton.interactable = true;
        });

        rebind.Start();
    }
}