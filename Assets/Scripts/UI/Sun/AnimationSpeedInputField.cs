using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace Netherlands3D.Twin
{
    public class AnimationSpeedInputField : MonoBehaviour
    {
        private AnimationSpeedConverter converter;
        public UnityEvent<float> onSpeedParsed;
        private TMP_InputField inputField;
        private UI_FloatToTextValue floatToText;
            
        private void Awake()
        {
            inputField = GetComponent<TMP_InputField>();
            converter = GetComponent<AnimationSpeedConverter>();
            floatToText = GetComponent<UI_FloatToTextValue>();
        }

        public void ParseField(string inputValue)
        {
            if (float.TryParse(inputValue, out var parsedValue))
            {
                var convertedValue = converter.ConvertSpeed(parsedValue, converter.TargetUnits, AnimationSpeedConverter.SpeedUnits.SecondsPerSecond);
                onSpeedParsed.Invoke(convertedValue);
            }
        }

        public void SetInputField(float timeSpeed)
        {
            var convertedSpeed = converter.ConvertSpeed(timeSpeed, AnimationSpeedConverter.SpeedUnits.SecondsPerSecond, converter.TargetUnits);
            floatToText.SetFloatText(convertedSpeed);
        }

        public void ReparseField(AnimationSpeedConverter.SpeedUnits newUnits)
        {
            var oldUnits = converter.TargetUnits;
            converter.TargetUnits = newUnits;
            
            if (float.TryParse(inputField.text, out var parsedValue))
            {
                var convertedValue = converter.ConvertSpeed(parsedValue, oldUnits, converter.TargetUnits);
                floatToText.SetFloatText(convertedValue);
            }
        }

        public void SetInteractable(int dropdownValue)
        {
            inputField.interactable = dropdownValue != 0;
        }
        
        public void SetAnimationSpeed(int dropdownValue)
        {
            switch (dropdownValue)
            {
                case 0:
                    ReparseField(AnimationSpeedConverter.SpeedUnits.SecondsPerSecond);
                    inputField.text = "1"; //set speed to 1 when changing dropdown to realtime
                    break;
                case 1:
                    ReparseField(AnimationSpeedConverter.SpeedUnits.HoursPerSecond);
                    break;
            }
        }
    }
}