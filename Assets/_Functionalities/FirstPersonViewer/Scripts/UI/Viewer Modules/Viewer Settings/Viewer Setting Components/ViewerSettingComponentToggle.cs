using GG.Extensions;
using Netherlands3D.FirstPersonViewer.Layers;
using Netherlands3D.FirstPersonViewer.ViewModus;
using UnityEngine;
using UnityEngine.UI;

namespace Netherlands3D.FirstPersonViewer.UI
{
    public class ViewerSettingComponentToggle : ViewerSettingComponent
    {
        [SerializeField] private Toggle toggle;

        private ViewerSettingBool ViewerSettingBool => setting as ViewerSettingBool;
        private FirstPersonLayerPropertyData propertyData;

        public override void SetValue(object value)
        {
            SetValue((bool)value);
        }

        private void SetValue(bool value)
        {
            toggle.SetValue(value);
        }

        public void SetPropertyData(FirstPersonLayerPropertyData propertyData)
        {
            this.propertyData = propertyData;
            ViewerSettingBool.movementSetting.OnValueChanged.AddListener(BoolValueChanged);
        }

        private void OnDestroy()
        {
            if (propertyData != null)
            {
                ViewerSettingBool.movementSetting.OnValueChanged.RemoveListener(BoolValueChanged);
                propertyData = null;
            }
        }

        private void BoolValueChanged(bool value)
        {
            propertyData.settingValues[setting.GetSettingName()] = value;
        }

        public void OnValueChanged(bool value)
        {
            ViewerSettingBool.InvokeOnValueChanged(value);
            SetValue(value);
        }
    }
}
