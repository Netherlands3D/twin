using Netherlands3D.FirstPersonViewer.Layers;
using Netherlands3D.FirstPersonViewer.ViewModus;
using TMPro;
using UnityEngine;

namespace Netherlands3D.FirstPersonViewer.UI
{
    public class ViewerSettingComponentInput : ViewerSettingComponent
    {
        [SerializeField] private TMP_InputField valueInput;
        private ViewerSettingValue ViewerSettingValue => setting as ViewerSettingValue;
        
        private string prevValue;
        private FirstPersonLayerPropertyData propertyData;

        public override void SetValue(object value)
        {
            prevValue = value.ToString();
            valueInput.text = value.ToString();
        }

        public override void SetPropertyData(FirstPersonLayerPropertyData propertyData)
        {
            this.propertyData = propertyData; 
            ViewerSettingValue.movementSetting.OnValueChanged.AddListener(OnValueChanged);
        }

        private void OnDestroy()
        {
            if (propertyData != null)
            {
                ViewerSettingValue.movementSetting.OnValueChanged.RemoveListener(OnValueChanged);
                propertyData = null;
            }
        }

        void OnValueChanged(float value)
        {
            propertyData.settingValues[setting.GetSettingName()] = value;
        }

        public void OnValueChanged(string value)
        {
            //We assume that the value is a float value. That's prob not a nice thing to do :/
            if (float.TryParse(value, out float newValue))
            {
                newValue = Mathf.Clamp(newValue, ViewerSettingValue.minValue, ViewerSettingValue.maxValue);

                setting.InvokeOnValueChanged(newValue);

                SetValue(newValue);
            }
            else valueInput.text = prevValue;
        }
    }
}
