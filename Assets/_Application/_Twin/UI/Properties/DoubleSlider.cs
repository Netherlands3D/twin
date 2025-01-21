using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Netherlands3D.Twin.UI.Properties
{
    public class DoubleSlider : MonoBehaviour
    {
        [SerializeField] private Slider minSlider;
        [SerializeField] private Slider maxSlider;
        [SerializeField] private bool readOnly;
        [SerializeField] private TMP_Text minValueField;
        [SerializeField] private TMP_Text maxValueField;
        [SerializeField] private TMP_InputField minInputField;
        [SerializeField] private TMP_InputField maxInputField;
        [SerializeField] private string unitOfMeasurement;

        [Header("Shared slider settings")] [SerializeField]
        private RectTransform fillArea;

        private RectTransform fill;

        [SerializeField] private float minPossibleValue;
        [SerializeField] private float maxPossibleValue = 1f;
        [SerializeField] private bool wholeNumbers;
        [SerializeField] private float minRange; //minimum difference between min and max slider values

        private DrivenRectTransformTracker m_Tracker;

        public float minSliderValue
        {
            get => minSlider.value;
            set
            {
                minSlider.value = value;
                UpdateVisuals();
            }
        }

        public float maxSliderValue
        {
            get => maxSlider.value;
            set
            {
                maxSlider.value = value;
                UpdateVisuals();
            }
        }

        public float normalizedMinValue
        {
            get
            {
                if (Mathf.Approximately(minPossibleValue, maxPossibleValue))
                    return 0;
                return Mathf.InverseLerp(minPossibleValue, maxPossibleValue, minSliderValue);
            }
            set { minSliderValue = Mathf.Lerp(minPossibleValue, maxPossibleValue, value); }
        }

        public float normalizedMaxValue
        {
            get
            {
                if (Mathf.Approximately(minPossibleValue, maxPossibleValue))
                    return 0;
                return Mathf.InverseLerp(minPossibleValue, maxPossibleValue, maxSliderValue);
            }
            set { maxSliderValue = Mathf.Lerp(minPossibleValue, maxPossibleValue, value); }
        }

        public UnityEvent<float> onMinValueChanged;
        public UnityEvent<float> onMaxValueChanged;

        private void Awake()
        {
            ApplySharedProperties();

            minValueField.gameObject.SetActive(readOnly);
            minInputField.gameObject.SetActive(!readOnly);
            maxValueField.gameObject.SetActive(readOnly);
            maxInputField.gameObject.SetActive(!readOnly);

            UpdateVisuals();
        }

        private void OnEnable()
        {
            minSlider.onValueChanged.AddListener(OnMinValueChanged);
            maxSlider.onValueChanged.AddListener(OnMaxValueChanged);

            minInputField.onValueChanged.AddListener(OnMinInputValueChanged);
            maxInputField.onValueChanged.AddListener(OnMaxInputValueChanged);
        }

        private void OnDisable()
        {
            minSlider.onValueChanged.RemoveListener(OnMinValueChanged);
            maxSlider.onValueChanged.RemoveListener(OnMaxValueChanged);

            minInputField.onValueChanged.RemoveListener(OnMinInputValueChanged);
            maxInputField.onValueChanged.RemoveListener(OnMaxInputValueChanged);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if(Application.isPlaying)
                UpdateVisuals();
        }

#endif
        private void ApplySharedProperties()
        {
            fill = fillArea.transform.GetChild(0).transform as RectTransform;

            minSlider.wholeNumbers = wholeNumbers;
            maxSlider.wholeNumbers = wholeNumbers;
            minSlider.minValue = minPossibleValue;
            maxSlider.minValue = minPossibleValue;
            minSlider.maxValue = maxPossibleValue;
            maxSlider.maxValue = maxPossibleValue;
        }

        private void OnMinInputValueChanged(string stringValue)
        {
            if (stringValue.EndsWith(unitOfMeasurement))
            {
                stringValue = stringValue.Substring(0, stringValue.Length - unitOfMeasurement.Length);
            }

            if (float.TryParse(stringValue, out float newValue))
            {
                minSlider.value = newValue;
            }
        }

        private void OnMaxInputValueChanged(string stringValue)
        {
            if (stringValue.EndsWith(unitOfMeasurement))
            {
                stringValue = stringValue.Substring(0, stringValue.Length - unitOfMeasurement.Length);
            }

            if (float.TryParse(stringValue, out float newValue))
            {
                maxSlider.value = newValue;
            }
        }

        private void OnMinValueChanged(float value)
        {
            if (value > maxSliderValue - minRange)
            {
                minSliderValue = maxSliderValue - minRange;
                return;
            }

            UpdateVisuals();
            onMinValueChanged.Invoke(value);
        }

        private void OnMaxValueChanged(float value)
        {
            if (value < minSliderValue + minRange)
            {
                maxSliderValue = value + minRange;
                return;
            }

            UpdateVisuals();
            onMaxValueChanged.Invoke(value);
        }

        // Force-update the slider. Useful if you've changed the properties and want it to update visually.
        private void UpdateVisuals()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
                ApplySharedProperties();
#endif

            m_Tracker.Clear();

            if (fill != null)
            {
                m_Tracker.Add(this, fill, DrivenTransformProperties.Anchors);
                Vector2 anchorMin = Vector2.zero;
                Vector2 anchorMax = Vector2.one;

                anchorMin.x = normalizedMinValue;
                anchorMax.x = normalizedMaxValue;

                fill.anchorMin = anchorMin;
                fill.anchorMax = anchorMax;
            }
            
            var valueText = minSliderValue.ToString("N2", CultureInfo.InvariantCulture) + unitOfMeasurement;
            minValueField.text = valueText;
            minInputField.SetTextWithoutNotify(valueText);
            
            valueText = maxSliderValue.ToString("N2", CultureInfo.InvariantCulture) + unitOfMeasurement;
            maxValueField.text = valueText;
            maxInputField.SetTextWithoutNotify(valueText);
        }

        public void SetMinValueWithoutNotify(float value)
        {
            if (value > maxSliderValue - minRange)
                value = maxSliderValue - minRange;
            
            minSlider.SetValueWithoutNotify(value);
            UpdateVisuals();
        }

        public void SetMaxValueWithoutNotify(float value)
        {
            if (value < minSliderValue - minRange)
                value = minSliderValue + minRange;
            
            maxSlider.SetValueWithoutNotify(value);
            UpdateVisuals();
        }
    }
}