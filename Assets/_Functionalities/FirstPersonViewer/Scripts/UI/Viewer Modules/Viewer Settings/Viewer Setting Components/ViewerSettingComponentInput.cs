using Netherlands3D.FirstPersonViewer.Events;
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

            if (float.TryParse(value, out float newValue))
            {
                newValue = Mathf.Clamp(newValue, input.minValue, input.maxValue);

                ViewerSettingsEvents<float>.Invoke(setting.settingsLabel, newValue);
                setting.OnValueChanged?.Invoke(newValue);
            } else valueInput.text = prevValue;
        }
    }
}
