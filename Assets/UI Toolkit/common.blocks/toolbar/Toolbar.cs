using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;

namespace Netherlands3D.Twin.UI
{
    public class Toolbar : MonoBehaviour
    {
        public UnityEvent<string> onActivateTool = new();
        public UnityEvent<string> onDeactivateTool = new();

        private RadioButton activeSelection = null;

        private void OnEnable()
        {
            var rootVisualElement = GetComponent<UIDocument>().rootVisualElement;
            List<RadioButton> radioButtons = rootVisualElement
                .Query<VisualElement>(classes:"toolbar__button")
                .OfType<RadioButton>()
                .ToList();

            radioButtons
                .ForEach(element =>
                {
                    element.RegisterCallback<ClickEvent>(ToggleRadioButton);
                    element.RegisterValueChangedCallback(OnRadioButtonChanged);
                });
        }

        private void ToggleRadioButton(ClickEvent evt)
        {
            ToggleRadioButton(evt.target as RadioButton);
        }

        private void ToggleRadioButton(RadioButton clickedRadioButton)
        {
            activeSelection = activeSelection == clickedRadioButton ? null : clickedRadioButton;
            
            // Uncheck the radiobutton if we click on the same one a second time
            if (activeSelection == null)
            {
                clickedRadioButton.value = false;
            }
        }

        private void OnRadioButtonChanged(ChangeEvent<bool> evt)
        {
            var radioButton = evt.target as RadioButton;
            if (radioButton == null) return;
            if (evt.previousValue == evt.newValue) return;

            var toolIdentifier = ExtractToolIdentifierFromRadioButton(radioButton);

            radioButton.EnableInClassList("toolbar__button--active", evt.newValue);
            switch (evt.newValue)
            {
                case true: onActivateTool.Invoke(toolIdentifier); break;
                default: onDeactivateTool.Invoke(toolIdentifier); break;
            }
        }

        private static string ExtractToolIdentifierFromRadioButton(RadioButton radioButton)
        {
            // If it is a template, the parent may contain the correct view data key
            var toolIdentifier = string.IsNullOrEmpty(radioButton.viewDataKey) == false
                ? radioButton.viewDataKey
                : radioButton.parent.viewDataKey;

            if (string.IsNullOrEmpty(toolIdentifier))
            {
                Debug.LogError(
                    "The tool button you just toggled does not have its ViewDataKey set, we expect a code that "
                    + "signals which tool is associated with this button"
                );
            }

            return toolIdentifier;
        }
    }
}
