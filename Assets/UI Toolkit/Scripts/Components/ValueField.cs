using Netherlands3D.UI.ExtensionMethods;
using Netherlands3D.UI_Toolkit.Scripts;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Components
{
    public enum ValueFieldStyle
    {
        Default,
        PropertiesPanel
    }

    [UxmlElement]
    public partial class ValueField : VisualElement
    {
        private const string StyleClassPrefix = "value-field-style-";
        private const string DefaultPlaceholder = "123456";

        private VisualElement labelContainer;
        private Label labelText;
        private TextInputBaseField<string> inputField;

        private string label = "X";
        private string placeholderText = DefaultPlaceholder;
        private ValueFieldStyle valueFieldStyle = ValueFieldStyle.Default;

        public VisualElement LabelContainer => labelContainer ??= this.Q<VisualElement>("LabelContainer");
        public Label LabelText => labelText ??= this.Q<Label>("LabelText");
        public TextInputBaseField<string> InputField => inputField ??= this.Q<TextInputBaseField<string>>("InputField");

        [UxmlAttribute("label-text")]
        public string Label
        {
            get => label;
            set
            {
                label = string.IsNullOrWhiteSpace(value) ? string.Empty : value;
                ApplyLabel();
            }
        }

        [UxmlAttribute("placeholder-text")]
        public string PlaceholderText
        {
            get => placeholderText;
            set
            {
                placeholderText = string.IsNullOrWhiteSpace(value) ? DefaultPlaceholder : value;
                ApplyPlaceholder();
            }
        }

        [UxmlAttribute("value-field-style")]
        public ValueFieldStyle Style
        {
            get => valueFieldStyle;
            set
            {
                valueFieldStyle = value;
                UpdateStyleClass();
                ApplyLabelTypography();
            }
        }

        public ValueField()
        {
            this.CloneComponentTree("Components");
            this.AddComponentStylesheet("Components");

            AddToClassList("value-field");

            RegisterCallback<AttachToPanelEvent>(_ =>
            {
                UpdateStyleClass();
                ApplyLabel();
                ApplyLabelTypography();
                ApplyPlaceholder();
            });
        }

        private void ApplyLabel()
        {
            if (LabelText == null) return;
            LabelText.text = label;
        }

        private void ApplyLabelTypography()
        {
            if (LabelText == null) return;

            // Clear both utilities first
            LabelText.RemoveFromClassList("text-header");
            LabelText.RemoveFromClassList("text-base");

            if (valueFieldStyle == ValueFieldStyle.Default)
                LabelText.AddToClassList("text-header");
            else
                LabelText.AddToClassList("text-base");
        }

        private void ApplyPlaceholder()
        {
            // Unity 6: placeholder is on textEdition
            if (InputField?.textEdition == null) return;
            InputField.textEdition.placeholder = placeholderText;
        }

        private void UpdateStyleClass()
        {
            this.ReplacePrefixedValueInClassList(
                StyleClassPrefix,
                valueFieldStyle.ToString().ToKebabCase()
            );
        }
    }
}