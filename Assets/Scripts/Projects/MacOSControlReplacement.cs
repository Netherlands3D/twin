using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class MacOSControlReplacement : MonoBehaviour
{
    [SerializeField] private InputActionAsset applicationActionMap;

    private void Awake()
    {
        // Check if we are running on macOS
        if (SystemInfo.operatingSystemFamily == OperatingSystemFamily.MacOSX)
        {
            Debug.Log("MacOS detected, replacing ctrl with cmd bindings");
            ReplaceCtrlWithCommand();
        }
    }

    private void ReplaceCtrlWithCommand()
    {
        // Iterate through all action maps in the input action asset
        foreach (var map in applicationActionMap.actionMaps)
        {
            // Iterate through all actions in each map
            foreach (var action in map.actions)
            {
                // Iterate through all bindings in each action
                for (int i = 0; i < action.bindings.Count; i++)
                {
                    var binding = action.bindings[i];

                    // Check if the binding has Ctrl (Control) as a modifier
                    if (!binding.isPartOfComposite)
                        continue;

                    if (binding.path.Contains("<Keyboard>/ctrl"))
                    {
                        Debug.LogWarning("compound control modifier found, cannot replace this with a compound cmd modifier because it does not exist. Using left cmd, make a input action map with both ctrl keys separately to include the right cmd key.");
                        // Replace "ctrl" with "cmd" (command on Mac, represented as 'meta' in Unity)
                        var modifiedBindingPath = binding.overridePath = "<Keyboard>/leftMeta";
                        action.ApplyBindingOverride(i, modifiedBindingPath);
                        Debug.Log($"Replaced Ctrl with LeftCommand for action: {action.name} on binding: {binding.effectivePath}");
                    }
                    else if (binding.path.Contains("<Keyboard>/leftCtrl"))
                    {
                        // Replace "leftCtrl" with "leftCmd" (command on Mac, represented as 'meta' in Unity)
                        var modifiedBindingPath = binding.overridePath = "<Keyboard>/leftMeta";
                        action.ApplyBindingOverride(i, modifiedBindingPath);
                        Debug.Log($"Replaced LeftCtrl with LeftCommand for action: {action.name} on binding: {binding.effectivePath}");
                    }
                    else if (binding.path.Contains("<Keyboard>/rightCtrl"))
                    {
                        // Replace "rightCtrl" with "rightCmd" (command on Mac, represented as 'meta' in Unity)
                        var modifiedBindingPath = binding.overridePath = "<Keyboard>/rightMeta";
                        action.ApplyBindingOverride(i, modifiedBindingPath);
                        Debug.Log($"Replaced RightCtrl with RightCommand for action: {action.name} on binding: {binding.effectivePath}");
                    }
                }
            }
        }
    }
}