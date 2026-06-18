using System;
using System.Globalization;
using Netherlands3D.UI_Toolkit;
using Netherlands3D.UI.ExtensionMethods;
using UnityEngine;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Components
{
    public enum NumberFieldStyle
    {
        Default,
        Small
    }

    /// <summary>
    /// Determines how the underlying double value of a NumberField is displayed and parsed.
    /// </summary>
    public enum NumberFieldFormat
    {
        Double,
        Time
    }

    [UxmlElement]
    public partial class NumberField : VisualElement
    {
        private const string StyleClassPrefix = "number-field--style-";
        private const string DefaultPlaceholder = "-";
        private const string unparseableDecimalSeparator = ",";
        private const string parseableDecimalSeparator = ".";
        private const char timeSeparator = ':';

        private VisualElement labelContainer;
        private Label labelText;
        private TextInputBaseField<string> inputField;

        private string label = "X";
        private string placeholderText = DefaultPlaceholder;
        private bool placeholderExplicitlySet;
        private NumberFieldStyle numberFieldStyle = NumberFieldStyle.Default;
        private NumberFieldFormat valueFormat = NumberFieldFormat.Double;
        private float scrollStep = 1f;
        private string unitCharacter = string.Empty;
        private string formatString;
        private int decimalCount;
        private bool labelVisible;

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
                placeholderExplicitlySet = !string.IsNullOrWhiteSpace(value);
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
        
        [UxmlAttribute("value-format")]
        public NumberFieldFormat ValueFormat
        {
            get => valueFormat;
            set
            {
                valueFormat = value;

                // If the user hasn't set an explicit placeholder, switch to a sensible
                // default for the chosen format (e.g. "00:00" instead of "-").
                if (!placeholderExplicitlySet)
                {
                    placeholderText = DefaultPlaceholder;
                    ApplyPlaceholder();
                }
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
        
        [UxmlAttribute("label-visible")]
        public bool LabelVisible
        {
            get => labelVisible;
            set
            {
                labelVisible = value;
                LabelContainer.EnableInClassList(UtilityClassConstants.HIDDEN, !labelVisible);
                InputField.EnableInClassList("no-label", !labelVisible);
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
            InputField.SetValueWithoutNotify(FormatValue(newValue));

            using var changeEvent = NavigationSubmitEvent.GetPooled();
            changeEvent.target = InputField;
            InputField.SendEvent(changeEvent);

            evt.StopPropagation();
        }

        public void SetValueWithoutNotify(double newValue)
        {
            InputField.SetValueWithoutNotify(FormatValue(newValue));
        }
        
        public void SetValueWithoutNotify(DateTime dateTime)
        {
            var totalMinutes = dateTime.Hour * 60 + dateTime.Minute;
            SetValueWithoutNotify((double)totalMinutes);
        }
        
        private string FormatValue(double value)
        {
            string formatted = string.Empty;
            switch (valueFormat)
            {
                case NumberFieldFormat.Time:
                    formatted = FormatAsTime(value);
                    break;
                case NumberFieldFormat.Double:
                    formatted = value.ToString(formatString, CultureInfo.InvariantCulture);
                    break;
                default:
                    formatted = value.ToString(formatString, CultureInfo.InvariantCulture);
                    break;
            }

            return $"{formatted}{UnitCharacter}";
        }

        private static string FormatAsTime(double totalMinutes)
        {
            var isNegative = totalMinutes < 0;
            var absMinutes = Math.Abs(totalMinutes);

            var hours = (int)(absMinutes / 60);
            var minutes = (int)Math.Round(absMinutes % 60, MidpointRounding.AwayFromZero);

            // Rounding can push minutes to exactly 60 (e.g. 89.6 minutes -> 1h, 60m)
            if (minutes >= 60)
            {
                minutes -= 60;
                hours += 1;
            }

            var sign = isNegative ? "-" : string.Empty;
            return $"{sign}{hours:00}{timeSeparator}{minutes:00}";
        }

        public double GetValueAsDouble()
        {
            if (valueFormat == NumberFieldFormat.Time)
                return ParseTimeAsTotalMinutes(InputField.text);

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
        
        public DateTime GetValueAsTime(DateTime fallbackTime)
        {
            var minutes = GetValueAsDouble();
            if (minutes < 0)
            {
                SetValueWithoutNotify(fallbackTime);
                return  fallbackTime;
            }
            
            return DateTime.Today.AddMinutes(minutes);
        }

        private double ParseTimeAsTotalMinutes(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return -1d;

            var trimmed = text.Trim();
            if (UnitCharacter.Length > 0)
                trimmed = trimmed.Replace(UnitCharacter, string.Empty).Trim();

            var isNegative = trimmed.StartsWith("-");
            if (isNegative)
                trimmed = trimmed.Substring(1);

            var parts = trimmed.Split(timeSeparator);

            if (!int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var hours))
                return -1d;

            var minutes = 0;
            if (parts.Length > 1)
                int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out minutes);

            var totalMinutes = (double)(hours * 60 + minutes);
            return isNegative ? -totalMinutes : totalMinutes;
        }

        public int GetValueAsInt()
        {
            return (int)GetValueAsDouble();
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