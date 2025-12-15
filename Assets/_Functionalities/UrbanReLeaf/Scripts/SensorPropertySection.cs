using System;
using System.Collections.Generic;
using Netherlands3D.Functionalities.OBJImporter;
using Netherlands3D.Twin.Layers.ExtensionMethods;
using Netherlands3D.Twin.Layers.Properties;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Netherlands3D.Functionalities.UrbanReLeaf
{
    [PropertySection(typeof(SensorPropertyData))]
    public class SensorPropertySection : MonoBehaviour, IVisualizationWithPropertyData
    {
        [SerializeField] private TMP_Text startTimeYearField;
        [SerializeField] private TMP_InputField startTimeYearInputField;
        [SerializeField] private TMP_Text startTimeMonthField;
        [SerializeField] private TMP_InputField startTimeMonthInputField;
        [SerializeField] private TMP_Text startTimeDayField;
        [SerializeField] private TMP_InputField startTimeDayInputField;

        [SerializeField] private TMP_Text endTimeYearField;
        [SerializeField] private TMP_InputField endTimeYearInputField;
        [SerializeField] private TMP_Text endTimeMonthField;
        [SerializeField] private TMP_InputField endTimeMonthInputField;
        [SerializeField] private TMP_Text endTimeDayField;
        [SerializeField] private TMP_InputField endTimeDayInputField;

        [SerializeField] private string formatString = "N2";

        [SerializeField] private Slider minSlider;
        [SerializeField] private Slider maxSlider;
        [SerializeField] private ColorPicker minimumColorPicker;
        [SerializeField] private ColorPicker maximumColorPicker;
        
        [SerializeField] private Button resetButton;
        
        private SensorPropertyData propertyData;
        
        
        public void LoadProperties(List<LayerPropertyData> properties)
        {
            propertyData = properties.Get<SensorPropertyData>();
            
            propertyData.OnMinValueChanged.AddListener(UpdateMinimumSlider);
            propertyData.OnMaxValueChanged.AddListener(UpdateMaximumSlider);
            propertyData.OnMinColorChanged.AddListener(UpdateMinimumColor);
            propertyData.OnMaxColorChanged.AddListener(UpdateMaximumColor);
            propertyData.OnStartDateChanged.AddListener(UpdateStartDate);
            propertyData.OnEndDateChanged.AddListener(UpdateEndDate);
            
            UpdateStartDate(propertyData.StartDate);
            UpdateEndDate(propertyData.EndDate);
            UpdateMinimumSlider(propertyData.MinValue);
            UpdateMaximumSlider(propertyData.MaxValue);
            UpdateMinimumColor(propertyData.MinColor);
            UpdateMaximumColor(propertyData.MaxColor);
        }

        private void Awake()
        {
            startTimeYearInputField.onValueChanged.AddListener(v => { startTimeYearField.text = v; HandleStartDate(); });
            startTimeMonthInputField.onValueChanged.AddListener(v => { startTimeMonthField.text = v; HandleStartDate(); });
            startTimeDayInputField.onValueChanged.AddListener(v => { startTimeDayField.text = v; HandleStartDate(); });

            endTimeYearInputField.onValueChanged.AddListener(v => { endTimeYearField.text = v; HandleEndDate(); });
            endTimeMonthInputField.onValueChanged.AddListener(v => { endTimeMonthField.text = v; HandleEndDate(); });
            endTimeDayInputField.onValueChanged.AddListener(v => { endTimeDayField.text = v; HandleEndDate(); });
        }

        private void OnEnable()
        { 
            minSlider?.onValueChanged.AddListener(HandleMinimum);
            maxSlider?.onValueChanged.AddListener(HandleMaximum);
            if(minimumColorPicker != null)
                minimumColorPicker.onColorChanged += HandleMinimumColor;
            if(maximumColorPicker != null)
                maximumColorPicker.onColorChanged += HandleMaximumColor;
            
            resetButton.onClick.AddListener(HandleReset);
        }

        private void OnDisable()
        {
            minSlider?.onValueChanged.RemoveListener(HandleMinimum);
            maxSlider?.onValueChanged.RemoveListener(HandleMaximum);
            if(minimumColorPicker != null)
                minimumColorPicker.onColorChanged -= HandleMinimumColor;
            if(maximumColorPicker != null)
                maximumColorPicker.onColorChanged -= HandleMaximumColor;
            
            resetButton.onClick.RemoveListener(HandleReset);
        }

        private void OnDestroy()
        {
            propertyData.OnMinValueChanged.RemoveListener(UpdateMinimumSlider);
            propertyData.OnMaxValueChanged.RemoveListener(UpdateMaximumSlider);
            propertyData.OnMinColorChanged.RemoveListener(UpdateMinimumColor);
            propertyData.OnMaxColorChanged.RemoveListener(UpdateMaximumColor);
            propertyData.OnStartDateChanged.RemoveListener(UpdateStartDate);
            propertyData.OnEndDateChanged.RemoveListener(UpdateEndDate);
        }

        private void HandleReset()
        {
            propertyData.OnResetValues.Invoke();
        }

        private void UpdateMinimumSlider(float value)
        {
            if(minSlider != null)
                minSlider.value = value;
        }
        
        private void UpdateMaximumSlider(float value)
        {
            if (maxSlider != null)
                maxSlider.value = value;
        }

        private void UpdateMinimumColor(Color color)
        {
            if (minimumColorPicker != null)
                minimumColorPicker.color = color;
        }

        private void UpdateMaximumColor(Color  color)
        {
            if (maximumColorPicker != null)
                maximumColorPicker.color = color;
        }

        private void UpdateStartDate(DateTime startDate)
        {
            startTimeYearField.text = startDate.Year.ToString();
            startTimeMonthField.text = startDate.Month.ToString();
            startTimeDayField.text = startDate.Day.ToString();

            startTimeYearInputField.text = startTimeYearField.text;
            startTimeMonthInputField.text = startTimeMonthField.text;
            startTimeDayInputField.text = startTimeDayField.text;
        }

        private void UpdateEndDate(DateTime endDate)
        {
            endTimeYearField.text = endDate.Year.ToString();
            endTimeMonthField.text = endDate.Month.ToString();
            endTimeDayField.text = endDate.Day.ToString();

            endTimeYearInputField.text = endTimeYearField.text;
            endTimeMonthInputField.text = endTimeMonthField.text;
            endTimeDayInputField.text = endTimeDayField.text;
        }

        private void HandleMinimum(float newValue) 
        {
            if(propertyData == null) return;
            
            if(newValue !=  propertyData.MinValue)
                propertyData.MinValue = newValue;            
        }

        private void HandleMaximum(float newValue) 
        {
            if(propertyData == null) return;
            
            if(newValue !=  propertyData.MaxValue)
                propertyData.MaxValue = newValue;            
        }

        private void HandleMinimumColor(Color newValue) 
        {
            if(propertyData == null) return;
            
            if(newValue !=  propertyData.MinColor)
                propertyData.MinColor  = newValue;
        }

        private void HandleMaximumColor(Color newValue) 
        {
            if(propertyData == null) return;
            
            if(newValue !=  propertyData.MaxColor)
                propertyData.MaxColor =  newValue;
        }

        private void HandleStartDate()
        {
            if (!IsValidYear(startTimeDayField.text, startTimeMonthField.text, startTimeYearField.text))
                return;

            startTimeDayInputField.text = startTimeDayField.text;
            startTimeMonthInputField.text = startTimeMonthField.text;
            startTimeYearInputField.text = startTimeYearField.text;
            
            int day = int.Parse(startTimeDayField.text);
            int month = int.Parse(startTimeMonthField.text);
            int year = int.Parse(startTimeYearField.text);
            DateTime newStart = new DateTime(year, month, day);
            if(newStart != propertyData.StartDate)
                propertyData.StartDate = newStart;
        }

        private void HandleEndDate()
        {
            if (!IsValidYear(endTimeDayField.text, endTimeMonthField.text, endTimeYearField.text))
                return;

            endTimeDayInputField.text = endTimeDayField.text;
            endTimeMonthInputField.text = endTimeMonthField.text;
            endTimeYearInputField.text = endTimeYearField.text;
            
            int day = int.Parse(endTimeDayField.text);
            int month = int.Parse(endTimeMonthField.text);
            int year = int.Parse(endTimeYearField.text);
            DateTime newEnd = new DateTime(year, month, day);
            if(newEnd != propertyData.EndDate)
                propertyData.EndDate = newEnd;
        }

        private bool IsValidYear(string day, string month, string year)
        {
            if (string.IsNullOrEmpty(day) || day.Length > 2)
                return false;

            if (string.IsNullOrEmpty(month) || month.Length > 2)
                return false;

            if (string.IsNullOrEmpty(year) || year.Length != 4)
                return false;

            return true;
        }
    }
}