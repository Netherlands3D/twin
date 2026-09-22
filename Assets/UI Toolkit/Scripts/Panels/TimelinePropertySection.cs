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
    [PropertySection(typeof(TimelineStylingLayerPropertyData), PropertySectionCategory.Styling)]
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

        private TimelineStylingLayerPropertyData timelineStylingPropertyData;

        public TimelinePropertySection()
        {
            this.CloneComponentTree("Panels");
            this.AddComponentStylesheet("Panels");

            buildStartCheckBox = this.Q<CheckboxToggle>("BuildStartCheckBox");
            buildEndCheckBox = this.Q<CheckboxToggle>("BuildEndCheckBox");
            demolishStartCheckBox = this.Q<CheckboxToggle>("DemolishStartCheckBox");
            demolishEndCheckBox = this.Q<CheckboxToggle>("DemolishEndCheckBox");

            // buildStartCheckBox.RegisterValueChangedCallback(OnBuildStartCheckBoxChanged);
            // buildEndCheckBox.RegisterValueChangedCallback(OnBuildEndCheckBoxChanged);
            // demolishStartCheckBox.RegisterValueChangedCallback(OnDemolishStartCheckBoxChanged);
            // demolishEndCheckBox.RegisterValueChangedCallback(OnDemolishEndCheckBoxChanged);
            
            buildStartDateField = this.Q<DateField>("BuildStart");
            buildEndDateField = this.Q<DateField>("BuildEnd");
            demolishStartDateField = this.Q<DateField>("DemolishStart");
            demolishEndDateField = this.Q<DateField>("DemolishEnd");

            // buildStartDateField.SubmitEvent += OnBuildStartInputFieldChanged;
            // buildEndDateField.SubmitEvent += OnBuildEndInputFieldChanged;
            // demolishStartDateField.SubmitEvent += OnDemolishStartInputFieldChanged;
            // demolishEndDateField.SubmitEvent += OnDemolishEndInputFieldChanged;
        }

        // private void OnBuildStartCheckBoxChanged(ChangeEvent<bool> evt)
        // {
        //     if(evt.newValue)
        //         timelineStylingPropertyData.BuildStart = DateTime.Now;
        //     else
        //         timelineStylingPropertyData.BuildStart = null;
        // }
        //
        // private void OnBuildEndCheckBoxChanged(ChangeEvent<bool> evt)
        // {
        //     if(evt.newValue)
        //         timelineStylingPropertyData.BuildEnd = DateTime.Now;
        //     else
        //         timelineStylingPropertyData.BuildEnd = null;
        // }
        //
        // private void OnDemolishStartCheckBoxChanged(ChangeEvent<bool> evt)
        // {
        //     if(evt.newValue)
        //         timelineStylingPropertyData.DemolishStart = DateTime.Now;
        //     else
        //         timelineStylingPropertyData.DemolishStart = null;
        // }
        //
        // private void OnDemolishEndCheckBoxChanged(ChangeEvent<bool> evt)
        // {
        //     if(evt.newValue)
        //         timelineStylingPropertyData.DemolishEnd = DateTime.Now;
        //     else
        //         timelineStylingPropertyData.DemolishEnd = null;
        // }
        //
        // private void OnBuildStartInputFieldChanged(int day, int month, int year)
        // {
        //     timelineStylingPropertyData.BuildStart = new DateTime(year, month, day);
        // }
        //
        // private void OnBuildEndInputFieldChanged(int day, int month, int year)
        // {
        //     timelineStylingPropertyData.BuildEnd = new DateTime(year, month, day);
        // }
        //
        // private void OnDemolishStartInputFieldChanged(int day, int month, int year)
        // {
        //     timelineStylingPropertyData.DemolishStart = new DateTime(year, month, day);
        // }
        //
        // private void OnDemolishEndInputFieldChanged(int day, int month, int year)
        // {
        //     timelineStylingPropertyData.DemolishEnd = new DateTime(year, month, day);
        // }

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
            timelineStylingPropertyData = properties.Get<TimelineStylingLayerPropertyData>();
            
            // timelineStylingPropertyData.OnBuildStartChanged.AddListener(OnBuildStartDateChanged);
            // timelineStylingPropertyData.OnBuildEndChanged.AddListener(OnBuildEndDateChanged);
            // timelineStylingPropertyData.OnDemolishStartChanged.AddListener(OnDemolishStartDateChanged);
            // timelineStylingPropertyData.OnDemolishEndChanged.AddListener(OnDemolishEndDateChanged);
            //
            // UpdateDateField(buildStartCheckBox, buildStartDateField, timelineStylingPropertyData.BuildStart);
            // UpdateDateField(buildEndCheckBox, buildEndDateField, timelineStylingPropertyData.BuildEnd);
            // UpdateDateField(demolishStartCheckBox ,demolishStartDateField, timelineStylingPropertyData.DemolishStart);
            // UpdateDateField(demolishEndCheckBox, demolishEndDateField, timelineStylingPropertyData.DemolishEnd);
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