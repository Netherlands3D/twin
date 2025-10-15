using Netherlands3D.Events;
using Netherlands3D.FirstPersonViewer.Temp;
using TMPro;
using UnityEngine;

namespace Netherlands3D
{
    public abstract class ViewerSettingComponent : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI settingNameText;
        [SerializeField] private TextMeshProUGUI settingUnitText;

        protected ViewerSetting setting;

        public virtual void Init(ViewerSetting setting)
        {
            this.setting = setting;

            settingNameText.text = setting.displayName;

            if (setting.units == null) settingUnitText.gameObject.SetActive(false);
            else settingUnitText.text = setting.units;

            //ViewerSettingsEvents<>
            setting.OnValueChanged += SetValue;

            SetValue(setting.GetValue());
        }

        private void OnDestroy()
        {
            setting.OnValueChanged -= SetValue;
        }

        public abstract void SetValue(object value);
    }
}
