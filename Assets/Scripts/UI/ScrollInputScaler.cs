using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

namespace Netherlands3D.Twin
{
    public class ScrollInputScaler : MonoBehaviour
    {
        [SerializeField] private float macScrollScaleValue = 0.2f;

        private void Start()
        {
#if !UNITY_EDITOR
            var eventSystem = EventSystem.current;
            var uiInput = eventSystem.GetComponent<InputSystemUIInputModule>();
            var uiActionMap = uiInput.actionsAsset.FindActionMap("UI");
            var scrollAction = uiActionMap.FindAction("ScrollWheel");

            if (SystemInfo.operatingSystemFamily == OperatingSystemFamily.MacOSX)
                ApplyInputActionScaling(scrollAction, macScrollScaleValue);
#endif
        }

        private void ApplyInputActionScaling(InputAction action, float scaleValue)
        {
            for (int i = 0; i < action.bindings.Count; i++)
            {
                var binding = action.bindings[i];
                binding.overrideProcessors = "scaleVector2(x=" + scaleValue + ",y=" + scaleValue + ")";
                action.ChangeBinding(i).To(binding);
                Debug.Log("scaling " + action.name + " input by: " + scaleValue);
            }
        }
    }
}