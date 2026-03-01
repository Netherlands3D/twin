using Netherlands3D.UI.ExtensionMethods;
using UnityEngine;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Components
{
    [UxmlElement]
    public partial class Slider : UnityEngine.UIElements.Slider
    {
        public enum HeaderType
        {
            Normal,
            NoHeader
        }

        private HeaderType headerType = HeaderType.Normal;

        [UxmlAttribute("header-type")]
        public HeaderType Header
        {
            get => headerType;
            set { headerType = value; UpdateClassList(); }
        }

        public Slider()
        {
            this.CloneComponentTree("Components");
            this.AddComponentStylesheet("Components");

            if (string.IsNullOrEmpty(label))
                label = "Label";

            showInputField = true;
            fill = true;
            
            RegisterCallback<AttachToPanelEvent>(_ => ApplyTextFieldClassToInput());
        }

        private void ApplyTextFieldClassToInput()
        {
            var inputText = this.Q<TextElement>(className: "unity-text-element--inner-input-field-component");
            if (inputText == null)
                return;

            inputText.AddToClassList("text-base");

            var input = this.Q<VisualElement>(className: "unity-base-text-field");
            if (input == null)
                return;

            input.AddToClassList("textfield");
        }

        private void UpdateClassList()
        {
            this.ReplacePrefixedValueInClassList("slider-header-", headerType.ToString().ToKebabCase());
        }
    }
}