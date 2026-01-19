using System;
using System.Collections.Generic;
using Netherlands3D.FirstPersonViewer.Layers;
using Netherlands3D.FirstPersonViewer.ViewModus;
using Netherlands3D.Twin.Layers.Properties;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace Netherlands3D.FirstPersonViewer.UI
{
    public class ViewerSettingComponentInput : ViewerSettingComponent
    {
        [SerializeField] private TMP_InputField valueInput;
        ViewerSettingValue viewerSettingValue => setting as ViewerSettingValue;
        
        private string prevValue;
        private FirstPersonLayerPropertyData propertyData;

        public override void SetValue(object value)
        {
            prevValue = value.ToString();
            valueInput.text = value.ToString();
        }

        public void SetPropertyData(FirstPersonLayerPropertyData propertyData)
        {
            this.propertyData = propertyData; 
            viewerSettingValue.movementSetting.OnValueChanged.AddListener(OnValueChanged);
        }

        private void OnDestroy()
        {
            if (propertyData != null)
            {
                viewerSettingValue.movementSetting.OnValueChanged.RemoveListener(OnValueChanged);
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
                newValue = Mathf.Clamp(newValue, viewerSettingValue.minValue, viewerSettingValue.maxValue);

                setting.InvokeOnValueChanged(newValue);

                SetValue(newValue);
            }
            else valueInput.text = prevValue;
        }
    }
}
