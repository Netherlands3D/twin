using Netherlands3D.FirstPersonViewer.Events;
using Netherlands3D.FirstPersonViewer.ViewModus;
using TMPro;
using UnityEngine;

namespace Netherlands3D.FirstPersonViewer.UI
{
    public class ViewerSettingComponentLabel : ViewerSettingComponent
    {
        [SerializeField] private TextMeshProUGUI valueLabelText;

        public override void Init(ViewerSetting setting)
        {
            base.Init(setting);
            ViewerSettingsEvents<string>.AddListener(setting.settingsLabel, SetValue);
        }

        private void OnEnable()
        {
            if (setting != null)  ViewerSettingsEvents<string>.AddListener(setting.settingsLabel, SetValue);
        }

        private void OnDisable()
        {
            ViewerSettingsEvents<string>.RemoveListener(setting.settingsLabel, SetValue);
        }

        public void SetValue(string value)
        {
            valueLabelText.text = value;
        }

        public override void SetValue(object value)
        {
            SetValue(value.ToString());
        }
    }
}
