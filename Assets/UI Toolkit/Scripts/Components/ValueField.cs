using Netherlands3D.UI.ExtensionMethods;
using Netherlands3D.UI_Toolkit.Scripts;
using UnityEngine;
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
        private float scrollStep = 1f;

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

        [UxmlAttribute("scroll-step")]
        public float ScrollStep
        {
            get => scrollStep;
            set => scrollStep = value;
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
                RegisterScrollCallback();
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
        
        private void RegisterScrollCallback()
        {
            if (InputField == null) return;

            InputField.RegisterCallback<WheelEvent>(OnScroll, TrickleDown.TrickleDown);
        }

        private void OnScroll(WheelEvent evt)
        {
            var direction = evt.delta.y > 0f ? -1f : 1f;

            var currentValue = 0f;
            if (!string.IsNullOrWhiteSpace(InputField.value))
                float.TryParse(InputField.value, System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out currentValue);

            var newValue = currentValue + direction * scrollStep;

            // We cannot set the InputField.text directly, we need to set it and manually invoke the change events
            var oldValue = InputField.text;
            InputField.SetValueWithoutNotify(
                newValue.ToString(System.Globalization.CultureInfo.InvariantCulture)
            );

            using var changeEvent = ChangeEvent<string>.GetPooled(oldValue, InputField.value);
            changeEvent.target = InputField;
            InputField.SendEvent(changeEvent);

            evt.StopPropagation();
        }
    }
}