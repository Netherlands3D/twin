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
                    if (!binding.isPartOfComposite || !binding.path.Contains("<Keyboard>/ctrl"))
                        continue;

                    // Replace "ctrl" with "cmd" (command on Mac, represented as 'meta' in Unity)
                    var modifiedBindingPath = binding.overridePath = "<Keyboard>/meta";
                    action.ApplyBindingOverride(i, modifiedBindingPath);

                    Debug.Log($"Replaced Ctrl with Command for action: {action.name} on binding: {binding.effectivePath}");
                }
            }
        }
    }
}