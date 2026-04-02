using System.Globalization;
using Netherlands3D.UI.ExtensionMethods;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Components
{
    public enum NumberFieldStyle
    {
        Default,
        Small
    }

    [UxmlElement]
    public partial class NumberField : VisualElement
    {
        private const string StyleClassPrefix = "number-field--style-";
        private const string DefaultPlaceholder = "-";
        private const string unparseableDecimalSeparator = ",";
        private const string parseableDecimalSeparator = ".";

        private VisualElement labelContainer;
        private Label labelText;
        private TextInputBaseField<string> inputField;

        private string label = "X";
        private string placeholderText = DefaultPlaceholder;
        private NumberFieldStyle numberFieldStyle = NumberFieldStyle.Default;
        private float scrollStep = 1f;
        private string unitCharacter = string.Empty;
        private string formatString;
        private int decimalCount;

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

        [UxmlAttribute("number-field-style")]
        public NumberFieldStyle Style
        {
            get => numberFieldStyle;
            set
            {
                numberFieldStyle = value;
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

        [UxmlAttribute("unit-character")]
        public string UnitCharacter
        {
            get => unitCharacter;
            set => unitCharacter = value;
        }
        
        [UxmlAttribute("decimal-count")]
        public int DecimalCount
        {
            get => decimalCount;
            set
            {
                decimalCount = value;
                formatString = GetFormatString(value);
            }
        }


        public NumberField()
        {
            this.CloneComponentTree("Components");
            this.AddComponentStylesheet("Components");

            AddToClassList("number-field");

            RegisterCallback<AttachToPanelEvent>(_ =>
            {
                UpdateStyleClass();
                ApplyLabel();
                ApplyLabelTypography();
                ApplyPlaceholder();
                RegisterScrollCallback();
                formatString = GetFormatString(DecimalCount);
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

            if (numberFieldStyle == NumberFieldStyle.Default)
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
                numberFieldStyle.ToString().ToKebabCase()
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
            
            var currentValue = GetValueAsDouble();
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

        public void SetValueWithoutNotify(double newValue)
        {
            InputField.SetValueWithoutNotify($"{newValue.ToString(formatString, CultureInfo.InvariantCulture)}{UnitCharacter}");
        }
        
        public double GetValueAsDouble()
        {
            //remove the unit character and set the correct decimal separator
            var numberFormat = new NumberFormatInfo
            {
                NumberDecimalSeparator = parseableDecimalSeparator
            };
            
            var text = InputField.text.Replace(unparseableDecimalSeparator, parseableDecimalSeparator);
            if(UnitCharacter.Length > 0)
            text = text.Replace(UnitCharacter, string.Empty);

            double.TryParse(text, NumberStyles.Float, numberFormat, out var value);

            return value;
        }
        
        private static string GetFormatString(int decimals)
        {
            if (decimals == 0)
                return "0";

            string zeros = new string('#', decimals);
            return $"0.{zeros}";
        }
    }
}