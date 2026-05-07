using System;
using System.Text;
using Netherlands3D.UI.ExtensionMethods;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Components
{
    [UxmlElement]
    public partial class TimeField : VisualElement
    {
        public event Action<int, int> TimeChanged;

        private const string InvalidClassName = "time-field--invalid";
        private string lastValidValue = string.Empty;

        private TextField inputField;
        private TextField InputField => inputField ??= this.Q<TextField>("InputField");

        [UxmlAttribute("value")]
        public string Value
        {
            get => InputField.value;
            set => SetValueWithoutNotify(value);
        }

        public TimeField()
        {
            this.CloneComponentTree("Components");
            this.AddComponentStylesheet("Components");

            InputField.RegisterValueChangedCallback(evt => OnInputChanged(evt.newValue));
            InputField.RegisterCallback<BlurEvent>(_ => CommitCurrentValue());
            InputField.RegisterCallback<KeyDownEvent>(OnInputKeyDown);
        }

        public void SetValueWithoutNotify(string value)
        {
            if (TryParseTime(value, out _, out _, out var normalizedValue))
            {
                lastValidValue = normalizedValue;
                InputField.SetValueWithoutNotify(normalizedValue);
                EnableInClassList(InvalidClassName, false);
                return;
            }

            lastValidValue = string.Empty;
            InputField.SetValueWithoutNotify(string.Empty);
            EnableInClassList(InvalidClassName, false);
        }

        private void OnInputChanged(string rawInput)
        {
            var sanitizedInput = SanitizeInput(rawInput);
            if (!string.Equals(rawInput, sanitizedInput, StringComparison.Ordinal))
                InputField.SetValueWithoutNotify(sanitizedInput);

            if (!TryParseTime(sanitizedInput, out var hour, out var minute, out var normalizedValue))
            {
                EnableInClassList(InvalidClassName, sanitizedInput.Length > 0);
                return;
            }

            lastValidValue = normalizedValue;
            EnableInClassList(InvalidClassName, false);
            TimeChanged?.Invoke(hour, minute);
        }

        private void OnInputKeyDown(KeyDownEvent evt)
        {
            if (evt.keyCode != UnityEngine.KeyCode.Return && evt.keyCode != UnityEngine.KeyCode.KeypadEnter)
                return;

            CommitCurrentValue();
            evt.StopPropagation();
        }

        private void CommitCurrentValue()
        {
            if (TryParseTime(InputField.value, out _, out _, out var normalizedValue))
            {
                InputField.SetValueWithoutNotify(normalizedValue);
                lastValidValue = normalizedValue;
                EnableInClassList(InvalidClassName, false);
                return;
            }

            InputField.SetValueWithoutNotify(lastValidValue);
            EnableInClassList(InvalidClassName, false);
        }

        private static string SanitizeInput(string input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;

            var builder = new StringBuilder(5);
            var seenSeparator = false;
            var hourDigits = 0;
            var minuteDigits = 0;

            foreach (var currentChar in input)
            {
                if (char.IsDigit(currentChar))
                {
                    if (!seenSeparator)
                    {
                        if (hourDigits >= 2) continue;
                        hourDigits++;
                    }
                    else
                    {
                        if (minuteDigits >= 2) continue;
                        minuteDigits++;
                    }

                    builder.Append(currentChar);
                    continue;
                }

                if (!IsSeparator(currentChar) || seenSeparator || hourDigits == 0) continue;

                seenSeparator = true;
                builder.Append(':');
            }

            return builder.ToString();
        }

        private static bool TryParseTime(string input, out int hour, out int minute, out string normalizedValue)
        {
            hour = 0;
            minute = 0;
            normalizedValue = string.Empty;

            var sanitizedInput = SanitizeInput(input);
            var parts = sanitizedInput.Split(':');
            if (parts.Length != 2) return false;

            if (!int.TryParse(parts[0], out hour)) return false;
            if (!int.TryParse(parts[1], out minute)) return false;

            if (hour is < 0 or > 23) return false;
            if (minute is < 0 or > 59) return false;

            normalizedValue = $"{hour:00}:{minute:00}";
            return true;
        }

        private static bool IsSeparator(char currentChar)
        {
            return currentChar == ':' || currentChar == '.' || currentChar == ';' || currentChar == ',';
        }
    }
}

