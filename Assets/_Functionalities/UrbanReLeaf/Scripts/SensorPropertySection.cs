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
            
            DateTime startDate = propertyData.StartDate;
            startTimeYearField.text = startDate.Year.ToString();
            startTimeMonthField.text = startDate.Month.ToString();
            startTimeDayField.text = startDate.Day.ToString();

            DateTime endDate = propertyData.EndDate;
            endTimeYearField.text = endDate.Year.ToString();
            endTimeMonthField.text = endDate.Month.ToString();
            endTimeDayField.text = endDate.Day.ToString();

            if(minSlider != null)
                minSlider.value = propertyData.MinValue;
            if(maxSlider != null) 
                maxSlider.value = propertyData.MaxValue;
            if(minimumColorPicker != null)
                minimumColorPicker.color = propertyData.MinColor;
            if(maximumColorPicker != null)
                maximumColorPicker.color = propertyData.MaxColor;
        }


        private void Awake()
        {
            startTimeYearInputField.onValueChanged.AddListener(v => { startTimeYearField.text = v; OnInputStartTimeValueChanged(); });
            startTimeMonthInputField.onValueChanged.AddListener(v => { startTimeMonthField.text = v; OnInputStartTimeValueChanged(); });
            startTimeDayInputField.onValueChanged.AddListener(v => { startTimeDayField.text = v; OnInputStartTimeValueChanged(); });

            endTimeYearInputField.onValueChanged.AddListener(v => { endTimeYearField.text = v; OnInputEndTimeValueChanged(); });
            endTimeMonthInputField.onValueChanged.AddListener(v => { endTimeMonthField.text = v; OnInputEndTimeValueChanged(); });
            endTimeDayInputField.onValueChanged.AddListener(v => { endTimeDayField.text = v; OnInputEndTimeValueChanged(); });
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

        private void HandleReset()
        {
            propertyData.OnResetValues.Invoke();
        }

        private void HandleMinimum(float newValue) 
        {
            propertyData.MinValue = newValue;            
        }

        private void HandleMaximum(float newValue) 
        {
            propertyData.MaxValue = newValue;            
        }

        private void HandleMinimumColor(Color newValue) 
        {
            propertyData.MinColor  = newValue;
        }

        private void HandleMaximumColor(Color newValue) 
        {
            propertyData.MaxColor =  newValue;
        }

        private void OnInputStartTimeValueChanged()
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
            propertyData.StartDate =  newStart;
        }

        private void OnInputEndTimeValueChanged()
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