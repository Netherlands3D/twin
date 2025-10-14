using Netherlands3D.FirstPersonViewer.Temp;
using TMPro;
using UnityEngine;

namespace Netherlands3D
{
    public class ViewerSettingComponentInput : ViewerSettingComponent
    {
        [SerializeField] private TMP_InputField valueInput;

        public override void SetValue(object value)
        {
            valueInput.text = value.ToString();
        }

        public void OnValueChanged(string value)
        {
            ViewerSettingValue input = setting as ViewerSettingValue;

            if (float.TryParse(value, out float newValue))
            {
                newValue = Mathf.Clamp(newValue, input.minValue, input.maxValue);

                setting.OnValueChanged?.Invoke(value);
            }
        }
    }
}
