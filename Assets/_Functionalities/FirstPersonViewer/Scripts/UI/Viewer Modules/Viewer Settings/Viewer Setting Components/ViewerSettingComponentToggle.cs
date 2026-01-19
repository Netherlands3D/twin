using GG.Extensions;
using Netherlands3D.FirstPersonViewer.ViewModus;
using UnityEngine;
using UnityEngine.UI;

namespace Netherlands3D.FirstPersonViewer.UI
{
    public class ViewerSettingComponentToggle : ViewerSettingComponent
    {
        [SerializeField] private Toggle toggle;

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
            ViewerSettingBool input = setting as ViewerSettingBool;

            setting.InvokeOnValueChanged(value);
            SetValue(value);
        }
    }
}
