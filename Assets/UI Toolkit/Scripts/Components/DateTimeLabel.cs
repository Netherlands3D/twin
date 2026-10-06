using System;
using System.Collections.Generic;
using System.Globalization;
using Netherlands3D.Twin.Utility;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Components
{
    [UxmlElement]
    public partial class DateTimeLabel : VisualElement
    {

        private readonly Label displayLabel = new();
        
        private DateTime value = default(DateTime);
        private DateTimeUnit context;
        private DateTimeUnit precision;
        
        
        public DateTimeLabel()
        {
            Add(displayLabel);
        }

        public DateTime Value
        {
            get => value;
            set
            {
                this.value = value;
                UpdateLabel();
            }
        }

        
        [UxmlAttribute("value")]
        public string ValueString
        {
            set
            {
                if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var dateTime))
                {
                    this.Value = dateTime;
                }
            }
        }
        
        [UxmlAttribute]         
        public DateTimeUnit Context
        {
            get => context;
            set
            {
                context = value;
                UpdateLabel();
            }
        }
        
        [UxmlAttribute]     
        public DateTimeUnit Precision
        {
            get => precision;
            set
            {
                precision = value;
                UpdateLabel();
            }
        }

        private bool Includes(DateTimeUnit unit)
        {
            return (int)context <= (int)unit
                   && (int)unit <= (int)precision;
        }

        private void UpdateLabel()
        {
            if ((int)context > (int)precision)
            {
                displayLabel.text = "-";
                return;
            }
            
            var dateParts = new List<string>(3);
            var timeParts = new List<string>(3);
            
            if (Includes(DateTimeUnit.Day)) dateParts.Add("%d");
            if (Includes(DateTimeUnit.Month)) dateParts.Add("MMMM");
            if (Includes(DateTimeUnit.Year)) dateParts.Add("yyyy");

            if (Includes(DateTimeUnit.Hour)) timeParts.Add("HH");
            if (Includes(DateTimeUnit.Minute)) timeParts.Add("mm");
            if (Includes(DateTimeUnit.Second)) timeParts.Add("ss");

            var dateFormat = string.Join(" ", dateParts);
            var timeFormat = string.Join(":", timeParts);

            var format = $"{dateFormat} {timeFormat}".Trim();

            displayLabel.text = value.ToString(
                format,
                CultureInfo.CurrentCulture
            );
            
        }
    }
}