using Netherlands3D.FirstPersonViewer.ViewModus;
using TMPro;
using UnityEngine;

namespace Netherlands3D.FirstPersonViewer.UI
{
    public abstract class ViewerSettingComponent : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI settingNameText;
        [SerializeField] private TextMeshProUGUI settingUnitText;

        protected ViewerSetting setting;

        public virtual void Init(ViewerSetting setting)
        {
            this.setting = setting;

            settingNameText.text = setting.settingsLabel.displayName;

            if (setting.settingsLabel.units == null) settingUnitText.gameObject.SetActive(false);
            else settingUnitText.text = setting.settingsLabel.units;

            SetValue(setting.GetValue());
        }

        public abstract void SetValue(object value);
    }
}
