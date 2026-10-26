using System;
using System.Globalization;
using Netherlands3D.Twin.Utility;
using Netherlands3D.UI.ExtensionMethods;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Components
{
    [UxmlElement]
    public partial class TimelineLabel : VisualElement
    {
        public DateTimeLabel contextLabel;
        public DateTimeLabel precisionLabel;

        private bool isPrimary;
        private DateTimeUnit unitContext;
        private DateTimeUnit unitPrecision;
        private DateTime time;

        public TimelineLabel()
        {
            this.CloneComponentTree("Components");
            this.AddComponentStylesheet("Components");

            contextLabel = this.Q<DateTimeLabel>("ContextLabel");
            precisionLabel = this.Q<DateTimeLabel>("Label");
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
            contextLabel.EnableInClassList("hidden", false);
            precisionLabel.EnableInClassList("hidden", true);
            
            precisionLabel.Context = unitPrecision;
            precisionLabel.Precision = unitPrecision;
            precisionLabel.Value = time;
        }
        
    }

}