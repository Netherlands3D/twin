using System;
using Netherlands3D.UI.ExtensionMethods;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Components
{
    [UxmlElement]
    public partial class TimeField : VisualElement
    {
        public event Action<string> ValueChanged;

        private TextField inputField;
        private TextField InputField => inputField ??= this.Q<TextField>("InputField");

        [UxmlAttribute("value")]
        public string Value
        {
            get => InputField.value;
            set => InputField.SetValueWithoutNotify(value ?? string.Empty);
        }

        public TimeField()
        {
            this.CloneComponentTree("Components");
            this.AddComponentStylesheet("Components");
            
            InputField.RegisterValueChangedCallback(evt => ValueChanged?.Invoke(evt.newValue));
        }

        public void SetValueWithoutNotify(string value)
        {
            InputField.SetValueWithoutNotify(value ?? string.Empty);
        }
    }
}

