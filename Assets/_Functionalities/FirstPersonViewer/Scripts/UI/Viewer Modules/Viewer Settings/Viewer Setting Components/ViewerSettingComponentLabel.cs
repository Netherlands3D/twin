using Netherlands3D.FirstPersonViewer.ViewModus;
using TMPro;
using UnityEngine;

namespace Netherlands3D.FirstPersonViewer.UI
{
    public class ViewerSettingComponentLabel : ViewerSettingComponent
    {
        [SerializeField] private TextMeshProUGUI valueLabelText;

        [SerializeField] private MovementLabelSetting baseSetting;

        public override void Init(ViewerSetting setting)
        {
            base.Init(setting);
            
            ViewerSettingLabel settingLabel = setting as ViewerSettingLabel;
            
            if (baseSetting == null)
            {
                baseSetting = settingLabel.movementSetting as MovementLabelSetting;
            }

            baseSetting.OnValueChanged.AddListener(SetValue);
        }

        private void OnEnable()
        {
            //OnEnable happens before the Initialize so check if it's not null.
            if (baseSetting != null) baseSetting.OnValueChanged.AddListener(SetValue);
        }

        private void OnDisable()
        {
            if (baseSetting != null) baseSetting.OnValueChanged.RemoveListener(SetValue);
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
