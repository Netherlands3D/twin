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
        private CheckboxToggle buildStartCheckBox;
        private CheckboxToggle buildEndCheckBox;
        private CheckboxToggle demolishStartCheckBox;
        private CheckboxToggle demolishEndCheckBox;
            
        private DateField buildStartDateField;
        private DateField buildEndDateField;
        private DateField demolishStartDateField;
        private DateField demolishEndDateField;

        private TimelineLayerPropertyData timelinePropertyData;

        public TimelinePropertySection()
        {
            this.CloneComponentTree("Panels");
            this.AddComponentStylesheet("Panels");

            buildStartCheckBox = this.Q<CheckboxToggle>("BuildStartCheckBox");
            buildEndCheckBox = this.Q<CheckboxToggle>("BuildEndCheckBox");
            demolishStartCheckBox = this.Q<CheckboxToggle>("DemolishStartCheckBox");
            demolishEndCheckBox = this.Q<CheckboxToggle>("DemolishEndCheckBox");

            buildStartCheckBox.RegisterValueChangedCallback(OnBuildStartCheckBoxChanged);
            buildEndCheckBox.RegisterValueChangedCallback(OnBuildEndCheckBoxChanged);
            demolishStartCheckBox.RegisterValueChangedCallback(OnDemolishStartCheckBoxChanged);
            demolishEndCheckBox.RegisterValueChangedCallback(OnDemolishEndCheckBoxChanged);
            
            buildStartDateField = this.Q<DateField>("BuildStart");
            buildEndDateField = this.Q<DateField>("BuildEnd");
            demolishStartDateField = this.Q<DateField>("DemolishStart");
            demolishEndDateField = this.Q<DateField>("DemolishEnd");

            buildStartDateField.SubmitEvent += OnBuildStartInputFieldChanged;
            buildEndDateField.SubmitEvent += OnBuildEndInputFieldChanged;
            demolishStartDateField.SubmitEvent += OnDemolishStartInputFieldChanged;
            demolishEndDateField.SubmitEvent += OnDemolishEndInputFieldChanged;
        }

        private void OnBuildStartCheckBoxChanged(ChangeEvent<bool> evt)
        {
            if(evt.newValue)
                timelinePropertyData.BuildStart = DateTime.Now;
            else
                timelinePropertyData.BuildStart = null;
        }
        
        private void OnBuildEndCheckBoxChanged(ChangeEvent<bool> evt)
        {
            if(evt.newValue)
                timelinePropertyData.BuildEnd = DateTime.Now;
            else
                timelinePropertyData.BuildEnd = null;
        }
        
        private void OnDemolishStartCheckBoxChanged(ChangeEvent<bool> evt)
        {
            if(evt.newValue)
                timelinePropertyData.DemolishStart = DateTime.Now;
            else
                timelinePropertyData.DemolishStart = null;
        }
        
        private void OnDemolishEndCheckBoxChanged(ChangeEvent<bool> evt)
        {
            if(evt.newValue)
                timelinePropertyData.DemolishEnd = DateTime.Now;
            else
                timelinePropertyData.DemolishEnd = null;
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
            UpdateDateField(buildStartCheckBox, buildStartDateField, newDate);
            // timelinePropertyData.BuildStart = newDate;
        }
        
        private void OnBuildEndDateChanged(DateTime? newDate)
        {
            UpdateDateField(buildEndCheckBox, buildEndDateField, newDate);
            // timelinePropertyData.BuildEnd = newDate;
        }
        
        private void OnDemolishStartDateChanged(DateTime? newDate)
        {
            UpdateDateField(demolishStartCheckBox, demolishStartDateField, newDate);
            // timelinePropertyData.DemolishStart = newDate;
        }
        
        private void OnDemolishEndDateChanged(DateTime? newDate)
        {
            UpdateDateField(demolishEndCheckBox, demolishEndDateField, newDate);
            // timelinePropertyData.DemolishEnd = newDate;
        }

        public void LoadProperties(List<LayerPropertyData> properties)
        {
            timelinePropertyData = properties.Get<TimelineLayerPropertyData>();
            
            timelinePropertyData.OnBuildStartChanged.AddListener(OnBuildStartDateChanged);
            timelinePropertyData.OnBuildEndChanged.AddListener(OnBuildEndDateChanged);
            timelinePropertyData.OnDemolishStartChanged.AddListener(OnDemolishStartDateChanged);
            timelinePropertyData.OnDemolishEndChanged.AddListener(OnDemolishEndDateChanged);
            
            UpdateDateField(buildStartCheckBox, buildStartDateField, timelinePropertyData.BuildStart);
            UpdateDateField(buildEndCheckBox, buildEndDateField, timelinePropertyData.BuildEnd);
            UpdateDateField(demolishStartCheckBox ,demolishStartDateField, timelinePropertyData.DemolishStart);
            UpdateDateField(demolishEndCheckBox, demolishEndDateField, timelinePropertyData.DemolishEnd);
        }

        private void UpdateDateField(CheckboxToggle checkboxToggle, DateField field, DateTime? newDate)
        {
            checkboxToggle.SetValueWithoutNotify(newDate.HasValue);
            field.SetEnabled(newDate.HasValue);
            
            if (!newDate.HasValue)
                return;

            var newDateValue = newDate.Value;
            field.SetValueWithoutNotify(newDateValue.Day, newDateValue.Month, newDateValue.Year);
        }
    }
}