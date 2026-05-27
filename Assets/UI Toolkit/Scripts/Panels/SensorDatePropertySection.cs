using System;
using System.Collections.Generic;
using Netherlands3D.Functionalities.UrbanReLeaf;
using Netherlands3D.Twin.Layers.ExtensionMethods;
using Netherlands3D.Twin.Layers.Properties;
using Netherlands3D.UI.Components;
using Netherlands3D.UI.ExtensionMethods;
using UnityEngine.UIElements;
using Button = UnityEngine.UIElements.Button;

namespace Netherlands3D.UI.Panels
{
    [UxmlElement]
    [PropertySection(typeof(SensorPropertyData), PropertySectionCategory.Settings)]
    public partial class SensorDatePropertySection : VisualElement, IVisualizationWithPropertyData
    {
        private SensorPropertyData propertyData;

        private DateField startDateField;
        private DateField endDateField;
        private Button resetButton;
        
        public SensorDatePropertySection()
        {
            this.CloneComponentTree("Panels");
            this.AddComponentStylesheet("Panels");    
            
            startDateField = this.Q<DateField>("StartdatumField");
            endDateField = this.Q<DateField>("EnddatumField");

            startDateField.SubmitEvent += OnStartDateChanged;
            endDateField.SubmitEvent += OnEndDateChanged;
            
            resetButton = this.Q<Button>();
            resetButton.RegisterCallback<ClickEvent>(OnResetButtonClicked);
        }

        private void OnResetButtonClicked(ClickEvent evt)
        {
            propertyData.ResetDateValues();
        }

        private void OnStartDateChanged(int day, int month, int year)
        {
            propertyData.StartDate = DateField.ToDateTime(day, month, year);
        }
        
        private void OnEndDateChanged(int day, int month, int year)
        {
            propertyData.EndDate = DateField.ToDateTime(day, month, year);
        }

        public void LoadProperties(List<LayerPropertyData> properties)
        {
            propertyData = properties.Get<SensorPropertyData>();

            propertyData.OnStartDateChanged.AddListener(UpdateStartDateField);
            propertyData.OnEndDateChanged.AddListener(UpdateEndDateField);
            
            UpdateStartDateField(propertyData.StartDate);
            UpdateEndDateField(propertyData.EndDate);
        }
        
        private void UpdateStartDateField(DateTime startDate)
        {
            SetDate(startDateField, startDate);
        }

        private void UpdateEndDateField(DateTime endDate)
        {
            SetDate(endDateField, endDate);
        }
        
        private void SetDate(DateField field, DateTime date)
        {
            field.SetValueWithoutNotify(date.Day, date.Month, date.Year);
        }
    }
}