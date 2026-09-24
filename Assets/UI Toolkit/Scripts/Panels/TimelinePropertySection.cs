using System;
using System.Collections.Generic;
using Netherlands3D.Timeline;
using Netherlands3D.Twin.Layers.ExtensionMethods;
using Netherlands3D.Twin.Layers.LayerTypes.HierarchicalObject.Properties;
using Netherlands3D.Twin.Layers.Properties;
using Netherlands3D.UI.Components;
using Netherlands3D.UI.ExtensionMethods;
using UnityEngine.UIElements;
using ListView = Netherlands3D.UI.Components.ListView;

namespace Netherlands3D.UI.Panels
{
    [UxmlElement]
    [PropertySection(typeof(TimelineStylingLayerPropertyData), PropertySectionCategory.Styling)]
    public partial class TimelinePropertySection : VisualElement, IVisualizationWithPropertyData
    {
        // private CheckboxToggle buildStartCheckBox;
        // private CheckboxToggle buildEndCheckBox;
        // private CheckboxToggle demolishStartCheckBox;
        // private CheckboxToggle demolishEndCheckBox;
        //     
        // private DateField buildStartDateField;
        // private DateField buildEndDateField;
        // private DateField demolishStartDateField;
        // private DateField demolishEndDateField;

        private ListView listView;
        private TimelineStylingLayerPropertyData timelineStylingPropertyData;
        private List<string> statusses = new();

        public TimelinePropertySection()
        {
            this.CloneComponentTree("Panels");
            this.AddComponentStylesheet("Panels");

            listView = this.Q<ListView>("StatusList");
            listView.itemsSource = statusses;
            // buildStartCheckBox = this.Q<CheckboxToggle>("BuildStartCheckBox");
            // buildEndCheckBox = this.Q<CheckboxToggle>("BuildEndCheckBox");
            // demolishStartCheckBox = this.Q<CheckboxToggle>("DemolishStartCheckBox");
            // demolishEndCheckBox = this.Q<CheckboxToggle>("DemolishEndCheckBox");

            // buildStartCheckBox.RegisterValueChangedCallback(OnBuildStartCheckBoxChanged);
            // buildEndCheckBox.RegisterValueChangedCallback(OnBuildEndCheckBoxChanged);
            // demolishStartCheckBox.RegisterValueChangedCallback(OnDemolishStartCheckBoxChanged);
            // demolishEndCheckBox.RegisterValueChangedCallback(OnDemolishEndCheckBoxChanged);

            // buildStartDateField = this.Q<DateField>("BuildStart");
            // buildEndDateField = this.Q<DateField>("BuildEnd");
            // demolishStartDateField = this.Q<DateField>("DemolishStart");
            // demolishEndDateField = this.Q<DateField>("DemolishEnd");

            // buildStartDateField.SubmitEvent += OnBuildStartInputFieldChanged;
            // buildEndDateField.SubmitEvent += OnBuildEndInputFieldChanged;
            // demolishStartDateField.SubmitEvent += OnDemolishStartInputFieldChanged;
            // demolishEndDateField.SubmitEvent += OnDemolishEndInputFieldChanged;
        }
        
        private void OnDemolishEndDateChanged(DateTime? newDate)
        {
            // UpdateDateField(demolishEndCheckBox, demolishEndDateField, newDate);
            // timelinePropertyData.DemolishEnd = newDate;
        }

        public void LoadProperties(List<LayerPropertyData> properties)
        {
            timelineStylingPropertyData = properties.Get<TimelineStylingLayerPropertyData>();
            timelineStylingPropertyData.OnTimestampCollectionAdded.AddListener(OnTimestampCollectionAdded);
        }

        private void OnTimestampCollectionAdded(TimestampCollection newTimestampCollection)
        {
            foreach (var timestamp in newTimestampCollection.Timestamps)
            {
                if(!statusses.Contains(timestamp.value))
                {
                    AddStatusEntry(timestamp.value);
                }
            }
        }

        private void AddStatusEntry(string status)
        {
            statusses.Add(status);
            listView.Rebuild();
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