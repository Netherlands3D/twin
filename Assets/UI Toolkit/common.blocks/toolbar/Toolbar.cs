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
            rootVisualElement
                .Query<VisualElement>(classes:"toolbar__button").ToList().ForEach(element =>
                {
                    element.RegisterCallback<ClickEvent>(ClickRadioButton);
                    element.RegisterCallback<ChangeEvent<bool>>(ChangeRadioButton);
                });
        }

        private void ClickRadioButton(ClickEvent evt)
        {
            var clickedRadioButton = evt.target as RadioButton;
            ToggleRadioButton(clickedRadioButton);
        }

        private void ChangeRadioButton(ChangeEvent<bool> evt)
        {
            var radioButton = evt.target as RadioButton;
            if (radioButton == null) return;
            if (evt.previousValue == evt.newValue) return;

            var key = radioButton.viewDataKey;
            if (string.IsNullOrEmpty(key))
            {
                Debug.LogError("The tool button you just toggled does not have its ViewDataKey set");
            }

            radioButton.EnableInClassList("toolbar__button--active", evt.newValue);
            if (evt.newValue)
            {
                onActivateTool.Invoke(key);
            }
            else
            {
                onDeactivateTool.Invoke(key);
            }
        }

        private void ToggleRadioButton(RadioButton clickedRadioButton)
        {
            activeSelection = activeSelection == clickedRadioButton ? null : clickedRadioButton;
            
            // Uncheck the radiobutton if we click on it again
            if (activeSelection == null)
            {
                clickedRadioButton.value = false;
            }
        }
    }
}
