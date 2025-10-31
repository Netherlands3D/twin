using Netherlands3D.FirstPersonViewer.ViewModus;
using TMPro;
using UnityEngine;

namespace Netherlands3D.FirstPersonViewer.UI
{
    public class ViewerSettingComponentInput : ViewerSettingComponent
    {
        [SerializeField] private TMP_InputField valueInput;

        private string prevValue;

        public override void SetValue(object value)
        {
            prevValue = value.ToString();
            valueInput.text = value.ToString();
        }

        public void OnValueChanged(string value)
        {
            ViewerSettingValue input = setting as ViewerSettingValue;

            //We assume that the value is a float value. That's prob not a nice thing to do :/
            if (float.TryParse(value, out float newValue))
            {
                newValue = Mathf.Clamp(newValue, input.minValue, input.maxValue);

                setting.InvokeOnValueChanged(newValue);
                
                SetValue(newValue);
            } else valueInput.text = prevValue;
        }
    }
}
