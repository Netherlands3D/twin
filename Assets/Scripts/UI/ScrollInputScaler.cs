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
        [SerializeField] private float scrollScaleValue = 0.2f;
        [SerializeField] private bool useZoomScaleValue;
        private InputAction scrollAction;

        public bool UseZoomScaleValue
        {
            get => useZoomScaleValue;
            set
            {
                useZoomScaleValue = value;
                if (useZoomScaleValue)
                    ApplyInputActionScaling(scrollAction, scrollScaleValue);
                else
                    RemoveInputActionScaling(scrollAction);
            }
        }

        private string[] originalProcessors;

        private void Start()
        {
            var eventSystem = EventSystem.current;
            var uiInput = eventSystem.GetComponent<InputSystemUIInputModule>();
            var uiActionMap = uiInput.actionsAsset.FindActionMap("UI");
            scrollAction = uiActionMap.FindAction("ScrollWheel");

            SetOriginalProcessors(scrollAction);
#if !UNITY_EDITOR
        useZoomScaleValue = SystemInfo.operatingSystemFamily == OperatingSystemFamily.MacOSX;
            return;
#endif
            if (UseZoomScaleValue)
                ApplyInputActionScaling(scrollAction, scrollScaleValue);
        }
        
        private void SetOriginalProcessors(InputAction action)
        {
            originalProcessors = new string[action.bindings.Count];
            for (int i = 0; i < action.bindings.Count; i++)
            {
                var binding = action.bindings[i];
                originalProcessors[i] = binding.overrideProcessors;
            }
        }

        private void ApplyInputActionScaling(InputAction action, float scaleValue)
        {
            for (int i = 0; i < action.bindings.Count; i++)
            {
                var binding = action.bindings[i];
                if (string.IsNullOrEmpty(originalProcessors[i]))
                    binding.overrideProcessors = "scaleVector2(x=" + scaleValue + ",y=" + scaleValue + ")";
                else
                    binding.overrideProcessors = originalProcessors[i] + ", scaleVector2(x=" + scaleValue + ",y=" + scaleValue + ")";
                action.ChangeBinding(i).To(binding);
                Debug.Log("scaling " + action.name + " input by: " + scaleValue);
            }
        }

        private void RemoveInputActionScaling(InputAction action)
        {
            for (int i = 0; i < action.bindings.Count; i++)
            {
                var binding = action.bindings[i];
                binding.overrideProcessors = originalProcessors[i];
                action.ChangeBinding(i).To(binding);
                Debug.Log("Removing " + action.name + " scale value");
            }
        }
    }
}