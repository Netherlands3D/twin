using System.Globalization;
using Netherlands3D.UI_Toolkit;
using Netherlands3D.UI.ExtensionMethods;
using UnityEngine;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Components
{
    [UxmlElement]
    public partial class EditableNameField : VisualElement, INotifyValueChanged<string>
    {
        private Label label; // we will switch between label and input field
        private TextField inputField;
        private bool firstClickDone;
        private bool intervalExpired;
        private IVisualElementScheduledItem clickTimer;
        
        [UxmlAttribute("value")]
        public string value
        {
            get { return label.text; }
            set
            {
                if (label.text == value) return;
                using var evt = ChangeEvent<string>.GetPooled(label.text, value);
                evt.target = this;
                label.text = value;
                inputField.SetValueWithoutNotify(value);
                SendEvent(evt);
            }
        }

        [UxmlAttribute] public float ClickInterval { get; set; } = 0.5f;

        public void SetValueWithoutNotify(string newValue)
        {
            label.text = newValue;
            inputField.SetValueWithoutNotify(newValue);
        }
        
        public EditableNameField()
        {
            this.CloneComponentTree("Components");
            this.AddComponentStylesheet("Components");

            label = this.Q<Label>("Label");
            inputField = this.Q<TextField>("InputField");

            label.RegisterCallback<ClickEvent>(OnNameLabelClicked);

            inputField.RegisterCallback<BlurEvent>(OnNameInputFieldBlur, TrickleDown.TrickleDown);
            inputField.RegisterCallback<NavigationSubmitEvent>(OnNavigationSubmitted, TrickleDown.TrickleDown);

            // inputField.EnableInClassList(UtilityClassConstants.HIDDEN, true); //todo: find out why this doesn't work but the inline style does
            inputField.style.display = DisplayStyle.None;
        }

        public void StartEditing()
        {
            label.EnableInClassList(UtilityClassConstants.HIDDEN, true);
            // inputField.EnableInClassList(UtilityClassConstants.HIDDEN, false); //todo: find out why this doesn't work but the inline style does
            inputField.style.display = DisplayStyle.Flex;

            inputField.Focus();
        }
        
        private void StopEditing()
        {
            label.EnableInClassList(UtilityClassConstants.HIDDEN, false);
            // inputField.EnableInClassList(UtilityClassConstants.HIDDEN, true); //todo: find out why this doesn't work but the inline style does
            inputField.style.display = DisplayStyle.None;
            
            firstClickDone = false;
            intervalExpired = false;
            clickTimer?.Pause();
            
            value = inputField.text;
        }
        
        private void OnNameLabelClicked(ClickEvent evt)
        {
            if (!firstClickDone) 
            {
                //first click: start timer
                firstClickDone = true;
                intervalExpired = false;
                clickTimer = schedule.Execute(() => intervalExpired = true);
                clickTimer.ExecuteLater((long)(ClickInterval * 1000));
            }
            else if (intervalExpired)
            {
                // Second click after interval = start editing
                firstClickDone = false;
                intervalExpired = false;
                StartEditing();
            }
            // If neither cases are true: Second click before interval = double-click
        }

        private void OnNameInputFieldBlur(BlurEvent evt)
        {
            StopEditing();
        }

        private void OnNavigationSubmitted(NavigationSubmitEvent evt)
        {
            StopEditing();
        }
    }
}