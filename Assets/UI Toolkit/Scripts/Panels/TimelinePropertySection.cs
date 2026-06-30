using System;
using System.Collections.Generic;
using Netherlands3D.Twin.Layers.ExtensionMethods;
using Netherlands3D.Twin.Layers.LayerTypes.HierarchicalObject.Properties;
using Netherlands3D.Twin.Layers.Properties;
using Netherlands3D.UI.Components;
using Netherlands3D.UI.ExtensionMethods;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Panels
{
    [UxmlElement]
    [PropertySection(typeof(TimelineLayerPropertyData), PropertySectionCategory.Styling)]
    public partial class TimelinePropertySection : VisualElement, IVisualizationWithPropertyData
    {
        private DateField buildStartDateField;
        private DateField buildEndDateField;
        private DateField demolishStartDateField;
        private DateField demolishEndDateField;

        private TimelineLayerPropertyData timelinePropertyData;

        public TimelinePropertySection()
        {
            this.CloneComponentTree("Panels");
            this.AddComponentStylesheet("Panels");

            buildStartDateField = this.Q<DateField>("BuildStart");
            buildEndDateField = this.Q<DateField>("BuildEnd");
            demolishStartDateField = this.Q<DateField>("DemolishStart");
            demolishEndDateField = this.Q<DateField>("DemolishEnd");

            buildStartDateField.SubmitEvent += OnBuildStartInputFieldChanged;
            buildEndDateField.SubmitEvent += OnBuildEndInputFieldChanged;
            demolishStartDateField.SubmitEvent += OnDemolishStartInputFieldChanged;
            demolishEndDateField.SubmitEvent += OnDemolishEndInputFieldChanged;
        }

        private void OnBuildStartInputFieldChanged(int day, int month, int year)
        {
            timelinePropertyData.BuildStart = new DateTime(year, month, day);
        }
        
        private void OnBuildEndInputFieldChanged(int day, int month, int year)
        {
            timelinePropertyData.BuildEnd = new DateTime(year, month, day);
        }
        
        private void OnDemolishStartInputFieldChanged(int day, int month, int year)
        {
            timelinePropertyData.DemolishStart = new DateTime(year, month, day);
        }
        
        private void OnDemolishEndInputFieldChanged(int day, int month, int year)
        {
            timelinePropertyData.DemolishEnd = new DateTime(year, month, day);
        }

        private void OnBuildStartDateChanged(DateTime? newDate)
        {
            timelinePropertyData.BuildStart = newDate;
        }
        
        private void OnBuildEndDateChanged(DateTime? newDate)
        {
            timelinePropertyData.BuildEnd = newDate;
        }
        
        private void OnDemolishStartDateChanged(DateTime? newDate)
        {
            timelinePropertyData.DemolishStart = newDate;
        }
        
        private void OnDemolishEndDateChanged(DateTime? newDate)
        {
            timelinePropertyData.DemolishEnd = newDate;
        }

        public void LoadProperties(List<LayerPropertyData> properties)
        {
            timelinePropertyData = properties.Get<TimelineLayerPropertyData>();

            timelinePropertyData.OnBuildStartChanged.AddListener(OnBuildStartDateChanged);
            timelinePropertyData.OnBuildEndChanged.AddListener(OnBuildEndDateChanged);
            timelinePropertyData.OnDemolishStartChanged.AddListener(OnDemolishStartDateChanged);
            timelinePropertyData.OnDemolishEndChanged.AddListener(OnDemolishEndDateChanged);
            
            UpdateDateField(buildStartDateField, timelinePropertyData.BuildStart);
            UpdateDateField(buildEndDateField, timelinePropertyData.BuildEnd);
            UpdateDateField(demolishStartDateField,timelinePropertyData.DemolishStart);
            UpdateDateField(demolishEndDateField, timelinePropertyData.DemolishEnd);
            
        }

        private void UpdateDateField(DateField field, DateTime? newDate)
        {
            field.SetEnabled(newDate.HasValue);
            if (!newDate.HasValue)
                return;

            var newDateValue = newDate.Value;
            field.SetValueWithoutNotify(newDateValue.Day, newDateValue.Month, newDateValue.Year);
        }
    }
}