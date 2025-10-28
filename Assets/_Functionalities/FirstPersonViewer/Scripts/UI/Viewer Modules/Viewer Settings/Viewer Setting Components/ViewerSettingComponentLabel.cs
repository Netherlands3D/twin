using Netherlands3D.FirstPersonViewer.Events;
using Netherlands3D.FirstPersonViewer.ViewModus;
using TMPro;
using UnityEngine;

namespace Netherlands3D.FirstPersonViewer.UI
{
    public class ViewerSettingComponentLabel : ViewerSettingComponent
    {
        [SerializeField] private TextMeshProUGUI valueLabelText;
        private ViewerSettingLabel settingLabel;

        public override void Init(ViewerSetting setting)
        {
            base.Init(setting);

            settingLabel = setting as ViewerSettingLabel;
            settingLabel.movementSetting.OnValueChanged.AddListener(SetValue);
        }

        private void OnEnable()
        {
            //OnEnable happens before the Initialize so check if it's not null.
            if (settingLabel != null) settingLabel.movementSetting.OnValueChanged.AddListener(SetValue);
        }

        private void OnDisable()
        {
            settingLabel.movementSetting.OnValueChanged.RemoveListener(SetValue);
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
