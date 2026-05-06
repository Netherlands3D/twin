using System;
using Netherlands3D.UI.ExtensionMethods;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Components
{
    [UxmlElement]
    public partial class DateField : VisualElement
    {
        private NumberField dayInput;
        private NumberField monthInput;
        private NumberField yearInput;
        private DateTime currentValue = DateTime.MinValue;

        public event Action<int, int, int> SubmitEvent;

        private NumberField DayInput => dayInput ??= this.Q<NumberField>("DayInput");
        private NumberField MonthInput => monthInput ??= this.Q<NumberField>("MonthInput");
        private NumberField YearInput => yearInput ??= this.Q<NumberField>("YearInput");

        public DateField()
        {
            this.CloneComponentTree("Components");
            this.AddComponentStylesheet("Components");

            RegisterSubmitCallbacks(DayInput);
            RegisterSubmitCallbacks(MonthInput);
            RegisterSubmitCallbacks(YearInput);
        }

        public void SetValueWithoutNotify(int day, int month, int year)
        {
            currentValue = ToDateTime(day, month, year);
            ApplyCurrentValueToFields();
        }

        private void NotifySubmitted()
        {
            currentValue = ToDateTime(DayInput.GetValueAsInt(), MonthInput.GetValueAsInt(), YearInput.GetValueAsInt());
            ApplyCurrentValueToFields();
            SubmitEvent?.Invoke(currentValue.Day, currentValue.Month, currentValue.Year);
        }

        public static DateTime ToDateTime(int day, int month, int year)
        {
            var clamped = ClampDate(day, month, year);
            return new DateTime(clamped.year, clamped.month, clamped.day);
        }

        private static (int day, int month, int year) ClampDate(int day, int month, int year)
        {
            year = Clamp(year, 1, 9999);
            month = Clamp(month, 1, 12);
            var maxDay = DateTime.DaysInMonth(year, month);
            day = Clamp(day, 1, maxDay);
            return (day, month, year);
        }

        private static int Clamp(int value, int min, int max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        private void ApplyCurrentValueToFields()
        {
            DayInput.SetValueWithoutNotify(currentValue.Day);
            MonthInput.SetValueWithoutNotify(currentValue.Month);
            YearInput.SetValueWithoutNotify(currentValue.Year);
        }

        private void RegisterSubmitCallbacks(NumberField field)
        {
            field.InputField.RegisterCallback<BlurEvent>(_ => NotifySubmitted());
            field.InputField.RegisterCallback<NavigationSubmitEvent>(_ => NotifySubmitted(), TrickleDown.TrickleDown);
        }
    }
}

