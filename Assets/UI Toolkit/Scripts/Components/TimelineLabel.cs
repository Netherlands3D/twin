using System;
using System.Globalization;
using Netherlands3D.Twin.Utility;
using Netherlands3D.UI_Toolkit;
using Netherlands3D.UI.ExtensionMethods;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Components
{
    [UxmlElement]
    public partial class TimelineLabel : VisualElement
    {
        public DateTimeLabel contextLabel;
        public DateTimeLabel label;

        private bool isPrimary;
        private DateTimeUnit unitContext;
        private DateTimeUnit unitPrecision;
        private bool enableContextLabel = false;
        private DateTime time;

        public TimelineLabel()
        {
            this.CloneComponentTree("Components");
            this.AddComponentStylesheet("Components");

            contextLabel = this.Q<DateTimeLabel>("ContextLabel");
            label = this.Q<DateTimeLabel>("Label");
        }

        [UxmlAttribute]
        public DateTimeUnit UnitContext
        {
            get => unitContext;
            set
            {
                unitContext = value;
                UpdateLabels();
            }
        }
        
        [UxmlAttribute]
        public DateTimeUnit UnitPrecision
        {
            get => unitPrecision;
            set
            {
                unitPrecision = value;
                UpdateLabels();
            }

        }

        public bool EnableSubLabel
        {
            get => enableContextLabel;
            set
            {
                enableContextLabel = value;
                UpdateLabels();
            }
        }

        public DateTime Time
        {
            get => time;
            set
            {
                time = value;
                UpdateLabels();
            }
        }
        
        [UxmlAttribute("time")]
        private string TimeUxml
        {
            get => time.ToString("O");
            set
            {
                time = DateTime.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
                UpdateLabels();
            }
        }

        private void UpdateLabels()
        {
            
            label.EnableInClassList(UtilityClassConstants.HIDDEN, false);
            
            label.Context = unitPrecision;
            label.Precision = unitPrecision;
            label.Value = time;
            
            contextLabel.EnableInClassList(UtilityClassConstants.HIDDEN, !enableContextLabel || unitPrecision == DateTimeUnit.Year);

            if (enableContextLabel)
            {
                if (unitPrecision == DateTimeUnit.Month)
                {
                    contextLabel.Context = DateTimeUnit.Year;
                    contextLabel.Precision = DateTimeUnit.Year;
                }
                
                else if (unitPrecision == DateTimeUnit.Day)
                {
                    contextLabel.Context = DateTimeUnit.Month;
                    contextLabel.Precision = DateTimeUnit.Month;
                }
                
                else if (unitPrecision == DateTimeUnit.Hour)
                {
                    contextLabel.Context = DateTimeUnit.Month;
                    contextLabel.Precision = DateTimeUnit.Day;
                }
                
                else if (unitPrecision == DateTimeUnit.Minute)
                {
                    contextLabel.Context = DateTimeUnit.Month;
                    contextLabel.Precision = DateTimeUnit.Day;
                }
                
                else if (unitPrecision == DateTimeUnit.Second)
                {
                    contextLabel.Context = DateTimeUnit.Hour;
                    contextLabel.Precision = DateTimeUnit.Minute;
                }

                contextLabel.Value = time;

            }
        }
    }
}
