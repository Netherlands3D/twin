using Netherlands3D.CartesianTiles;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Netherlands3D.Twin.Layers.Properties
{
    public class SensorPropertySection : MonoBehaviour
    {
        private SensorDataController controller;
        private SensorProjectionLayer projectionLayer;

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

        private float defaultMinValue;
        private float defaultMaxValue;
        private Color defaultMinColor;
        private Color defaultMaxColor;

        public SensorDataController Controller
        {
            get
            {                
                return controller;
            }            
            set
            {
                controller = value;
                if (projectionLayer == null)
                {
                    projectionLayer = controller.gameObject.GetComponent<SensorProjectionLayer>();
                    defaultMinValue = controller.Minimum;
                    defaultMaxValue = controller.Maximum;
                    defaultMinColor = controller.MinColor;
                    defaultMaxColor = controller.MaxColor;
                }


                DateTime startDate = controller.StartDate;
                startTimeYearField.text = startDate.Year.ToString();
                startTimeMonthField.text = startDate.Month.ToString();
                startTimeDayField.text = startDate.Day.ToString();
                OnInputStartTimeValueChanged();

                DateTime endDate = controller.EndDate;
                endTimeYearField.text = endDate.Year.ToString();
                endTimeMonthField.text = endDate.Month.ToString();
                endTimeDayField.text = endDate.Day.ToString();
                OnInputEndTimeValueChanged();

                if(minSlider != null)
                    minSlider.value = controller.Minimum;
                if(maxSlider != null) 
                    maxSlider.value = controller.Maximum;
                if(minimumColorPicker != null)
                    minimumColorPicker.color = controller.MinColor;
                if(maximumColorPicker != null)
                    maximumColorPicker.color = controller.MaxColor;
            }
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
        }

        private void OnDisable()
        {
            minSlider?.onValueChanged.RemoveListener(HandleMinimum);
            maxSlider?.onValueChanged.RemoveListener(HandleMaximum);
            if(minimumColorPicker != null)
                minimumColorPicker.onColorChanged -= HandleMinimumColor;
            if(maximumColorPicker != null)
                maximumColorPicker.onColorChanged -= HandleMaximumColor;
        }       

        private void HandleMinimum(float newValue) 
        {
            if(newValue != controller.Minimum)
                projectionLayer.SetVisibleTilesDirty();
            controller.Minimum = newValue;            
        }

        private void HandleMaximum(float newValue) 
        {
            if(newValue != controller.Maximum)
                projectionLayer.SetVisibleTilesDirty();
            controller.Maximum = newValue;            
        }

        private void HandleMinimumColor(Color newValue) 
        {
            if (controller)
            {
                if(newValue != controller.MinColor)
                    projectionLayer.SetVisibleTilesDirty();
                controller.MinColor = newValue;                
            }
        }

        private void HandleMaximumColor(Color newValue) 
        {
            if (controller)
            {
                if(newValue != controller.MaxColor)
                    projectionLayer.SetVisibleTilesDirty();
                controller.MaxColor = newValue;                
            }
        }

        private void OnInputStartTimeValueChanged()
        {
            if (!IsValidYear(startTimeDayField.text, startTimeMonthField.text, startTimeYearField.text))
                return;

            startTimeDayInputField.text = startTimeDayField.text;
            startTimeMonthInputField.text = startTimeMonthField.text;
            startTimeYearInputField.text = startTimeYearField.text;

            projectionLayer.SetVisibleTilesDirty();
            int day = int.Parse(startTimeDayField.text);
            int month = int.Parse(startTimeMonthField.text);
            int year = int.Parse(startTimeYearField.text);
            DateTime endTime = controller.EndDate;
            DateTime newStart = new DateTime(year, month, day);
            controller.SetTimeWindow(newStart, endTime);
        }

        private void OnInputEndTimeValueChanged()
        {
            if (!IsValidYear(endTimeDayField.text, endTimeMonthField.text, endTimeYearField.text))
                return;

            endTimeDayInputField.text = endTimeDayField.text;
            endTimeMonthInputField.text = endTimeMonthField.text;
            endTimeYearInputField.text = endTimeYearField.text;

            projectionLayer.SetVisibleTilesDirty();
            int day = int.Parse(endTimeDayField.text);
            int month = int.Parse(endTimeMonthField.text);
            int year = int.Parse(endTimeYearField.text);
            DateTime startTime = controller.StartDate;
            DateTime newEnd = new DateTime(year, month, day);
            controller.SetTimeWindow(startTime, newEnd);
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

        public void ResetDefaultValues()
        {
            DateTime startDate = controller.DefaultStartDate;
            startTimeYearField.text = startDate.Year.ToString();
            startTimeMonthField.text = startDate.Month.ToString();
            startTimeDayField.text = startDate.Day.ToString();
            OnInputStartTimeValueChanged();

            DateTime endDate = controller.DefaultEndDate;
            endTimeYearField.text = endDate.Year.ToString();
            endTimeMonthField.text = endDate.Month.ToString();
            endTimeDayField.text = endDate.Day.ToString();
            OnInputEndTimeValueChanged();

            if (minSlider != null)
                minSlider.value = defaultMinValue;
            if (maxSlider != null)
                maxSlider.value = defaultMaxValue;
            if (minimumColorPicker != null)
                minimumColorPicker.color = defaultMinColor;
            if (maximumColorPicker != null)
                maximumColorPicker.color = defaultMaxColor;
        }
    }
}