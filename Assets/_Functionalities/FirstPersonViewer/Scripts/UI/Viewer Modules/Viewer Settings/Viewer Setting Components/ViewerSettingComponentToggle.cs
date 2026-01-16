using GG.Extensions;
using Netherlands3D.FirstPersonViewer.ViewModus;
using UnityEngine;
using UnityEngine.UI;

namespace Netherlands3D.FirstPersonViewer.UI
{
    public class ViewerSettingComponentToggle : ViewerSettingComponent
    {
        [SerializeField] private Toggle toggle;
        private ViewerSettingBool settingBool;

        public override void Init(ViewerSetting setting)
        {
            base.Init(setting);

            settingBool = setting as ViewerSettingBool;
            settingBool.movementSetting.OnValueChanged.AddListener(SetValue);
        }

        public void SetValue(bool value)
        {
            toggle.SetValue(value);
        }

        public override void SetValue(object value)
        {
            SetValue((bool)value);
        }

        public void OnValueChanged(bool value)
        {
            ViewerSettingValue input = setting as ViewerSettingValue;

            setting.InvokeOnValueChanged(value);
            SetValue(value);
        }
    }
}
