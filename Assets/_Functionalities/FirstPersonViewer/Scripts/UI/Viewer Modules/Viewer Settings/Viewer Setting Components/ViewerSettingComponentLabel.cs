using Netherlands3D.FirstPersonViewer.ViewModus;
using TMPro;
using UnityEngine;

namespace Netherlands3D.FirstPersonViewer.UI
{
    public class ViewerSettingComponentLabel : ViewerSettingComponent
    {
        [SerializeField] private TextMeshProUGUI valueLabelText;
        [SerializeField] private ViewerSettingLabel settingLabel;

        [SerializeField] private MovementLabelSetting baseSetting;

        private void Start()
        {
            if(baseSetting != null) baseSetting.OnValueChanged.AddListener(SetValue);
        }

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
            if (baseSetting != null) baseSetting.OnValueChanged.RemoveListener(SetValue);
            else settingLabel.movementSetting.OnValueChanged.RemoveListener(SetValue);
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
