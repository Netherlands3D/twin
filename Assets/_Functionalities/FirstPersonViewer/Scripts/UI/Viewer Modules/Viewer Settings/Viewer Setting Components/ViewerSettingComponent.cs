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

        private void Start()
        {
            if (setting != null) Init(setting);
        }

        public virtual void Init(ViewerSetting setting)
        {
            this.setting = setting;

            if (settingNameText != null) settingNameText.text = setting.GetDisplayName();

            if (settingUnitText != null) settingUnitText.text = setting.GetDisplayUnits();


            SetValue(setting.GetValue());
        }

        public abstract void SetValue(object value);
    }
}
