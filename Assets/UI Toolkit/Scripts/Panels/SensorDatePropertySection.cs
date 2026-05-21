using System;
using System.Collections.Generic;
using Netherlands3D.Functionalities.UrbanReLeaf;
using Netherlands3D.Twin.Layers.ExtensionMethods;
using Netherlands3D.Twin.Layers.Properties;
using Netherlands3D.UI.Components;
using Netherlands3D.UI.ExtensionMethods;
using UnityEngine;
using UnityEngine.UIElements;
using Button = UnityEngine.UIElements.Button;

namespace Netherlands3D.UI.Panels
{
    [UxmlElement]
    [PropertySection(typeof(SensorPropertyData), PropertySectionCategory.Settings)]
    public partial class SensorDatePropertySection : VisualElement, IVisualizationWithPropertyData
    {
        private SensorPropertyData propertyData;

        private XYZField startDateField;
        private XYZField endDateField;
        private Button resetButton;
        
        public SensorDatePropertySection()
        {
            this.CloneComponentTree("Panels");
            this.AddComponentStylesheet("Panels");    
            
            startDateField = this.Q<XYZField>("StartdatumField");
            endDateField = this.Q<XYZField>("EnddatumField");
            
            startDateField.xField.InputField.RegisterCallback<BlurEvent>(_ => OnStartDateChanged());
            startDateField.yField.InputField.RegisterCallback<BlurEvent>(_ => OnStartDateChanged());
            startDateField.zField.InputField.RegisterCallback<BlurEvent>(_ => OnStartDateChanged());
            endDateField.xField.InputField.RegisterCallback<BlurEvent>(_ => OnEndDateChanged());
            endDateField.yField.InputField.RegisterCallback<BlurEvent>(_ => OnEndDateChanged());
            endDateField.zField.InputField.RegisterCallback<BlurEvent>(_ => OnEndDateChanged());
            
            startDateField.xField.InputField.RegisterCallback<NavigationSubmitEvent>(_ => startDateField.xField.Focus(), TrickleDown.TrickleDown);
            startDateField.yField.InputField.RegisterCallback<NavigationSubmitEvent>(_ => startDateField.yField.Focus(), TrickleDown.TrickleDown);
            startDateField.zField.InputField.RegisterCallback<NavigationSubmitEvent>(_ => startDateField.zField.Focus(), TrickleDown.TrickleDown);
            endDateField.xField.InputField.RegisterCallback<NavigationSubmitEvent>(_ => endDateField.xField.Focus(), TrickleDown.TrickleDown);
            endDateField.yField.InputField.RegisterCallback<NavigationSubmitEvent>(_ => endDateField.yField.Focus(), TrickleDown.TrickleDown);
            endDateField.zField.InputField.RegisterCallback<NavigationSubmitEvent>(_ => endDateField.zField.Focus(), TrickleDown.TrickleDown);
            
            resetButton = this.Q<Button>();
            resetButton.RegisterCallback<ClickEvent>(OnResetButtonClicked);
        }

        private void OnResetButtonClicked(ClickEvent evt)
        {
            propertyData.ResetDateValues();
        }

        private void OnStartDateChanged()
        {
            var date = GetDateTime(startDateField);
            propertyData.StartDate = date;
        }
        
        private void OnEndDateChanged()
        {
            var date = GetDateTime(endDateField);
            propertyData.EndDate = date;
        }

        private DateTime GetDateTime(XYZField field)
        {
            var x = field.xField.GetValueAsInt();
            var y = field.yField.GetValueAsInt();
            var z = field.zField.GetValueAsInt();
            
            z = Mathf.Clamp(z, 1, 9999);
            y = Mathf.Clamp(y, 1, 12);
            var maxDay = DateTime.DaysInMonth(z, y);
            x = Mathf.Clamp(x, 1, maxDay);
            
            return new DateTime(z, y, x);
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
        
        private void SetDate(XYZField field, DateTime date)
        {
            field.xField.SetValueWithoutNotify(date.Day);
            field.yField.SetValueWithoutNotify(date.Month);
            field.zField.SetValueWithoutNotify(date.Year);
        }
    }
}