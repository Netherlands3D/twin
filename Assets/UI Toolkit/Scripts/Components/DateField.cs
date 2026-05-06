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

        public event Action<int, int, int> ValueChanged;

        private NumberField DayInput => dayInput ??= this.Q<NumberField>("DayInput");
        private NumberField MonthInput => monthInput ??= this.Q<NumberField>("MonthInput");
        private NumberField YearInput => yearInput ??= this.Q<NumberField>("YearInput");

        public DateField()
        {
            this.CloneComponentTree("Components");
            this.AddComponentStylesheet("Components");

            DayInput.InputField.RegisterValueChangedCallback(_ => NotifyValueChanged());
            MonthInput.InputField.RegisterValueChangedCallback(_ => NotifyValueChanged());
            YearInput.InputField.RegisterValueChangedCallback(_ => NotifyValueChanged());
        }

        public void SetValueWithoutNotify(int day, int month, int year)
        {
            DayInput.SetValueWithoutNotify(day);
            MonthInput.SetValueWithoutNotify(month);
            YearInput.SetValueWithoutNotify(year);
        }

        public int GetDay() => DayInput.GetValueAsInt();
        public int GetMonth() => MonthInput.GetValueAsInt();
        public int GetYear() => YearInput.GetValueAsInt();

        private void NotifyValueChanged()
        {
            ValueChanged?.Invoke(GetDay(), GetMonth(), GetYear());
        }
    }
}

