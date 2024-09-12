using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace Netherlands3D.Twin
{
    public class AnimationSpeedInputField : MonoBehaviour
    {
        [SerializeField] private int matchingDropdownIndex; //this determines when this inputField is active and should set the animation speed (when this value matches the value that is selected by the dropdown)

        private AnimationSpeedConverter converter;
        public UnityEvent<float> onSpeedParsed;
        private TMP_InputField inputField;
        private UI_FloatToTextValue floatToText;
        private AnimationSpeedIncrementer incrementer;
        private float speedInLocalUnits = 1; //seconds per second speed needed for Suntime.cs

        private void Awake()
        {
            inputField = GetComponent<TMP_InputField>();
            converter = GetComponent<AnimationSpeedConverter>();
            floatToText = GetComponent<UI_FloatToTextValue>();
            incrementer = GetComponent<AnimationSpeedIncrementer>();
        }

        public void ParseField(string inputValue)
        {
            if (float.TryParse(inputValue, out var parsedValue))
            {
                speedInLocalUnits = parsedValue;
                incrementer?.UpdateSpeedIndex(speedInLocalUnits);
                var convertedSpeed = converter.ConvertSpeed(parsedValue, converter.TargetUnits, AnimationSpeedConverter.SpeedUnits.SecondsPerSecond);
                onSpeedParsed.Invoke(convertedSpeed);
            }
        }

        public void SetInputField(float timeSpeed)
        {
            speedInLocalUnits = converter.ConvertSpeed(timeSpeed, AnimationSpeedConverter.SpeedUnits.SecondsPerSecond, converter.TargetUnits);
            incrementer?.UpdateSpeedIndex(speedInLocalUnits);
            floatToText.SetFloatText(speedInLocalUnits);
        }

        public void ToggleInputField(int dropdownValue)
        {
            gameObject.SetActive(dropdownValue == matchingDropdownIndex);
        }

        public void SetAnimationSpeed(int dropdownValue)
        {
            if (dropdownValue != matchingDropdownIndex)
                return;
            
            ParseField(speedInLocalUnits.ToString());
        }
    }
}