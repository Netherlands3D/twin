using Netherlands3D.FirstPersonViewer.UI;
using Netherlands3D.FirstPersonViewer.ViewModus;
using Netherlands3D.Services;
using Netherlands3D.Twin.Layers.ExtensionMethods;
using Netherlands3D.Twin.Layers.Properties;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace Netherlands3D.FirstPersonViewer.Layers
{
    [PropertySection(typeof(FirstPersonLayerPropertyData))]
    public class FirstPersonPropertySection : MonoBehaviour, IVisualizationWithPropertyData
    {
        private FirstPersonLayerPropertyData firstPersonData;

        [SerializeField] private ViewerSettingComponentsList componentList;
        [SerializeField] private Transform settingParent;

        [SerializeField] private TMP_Dropdown modusDropdown;

        private List<(ViewerSettingValue setting, UnityAction<float> callback)> addedCallbacks = new List<(ViewerSettingValue setting, UnityAction<float> callback)>();

        public void LoadProperties(List<LayerPropertyData> properties)
        {
            firstPersonData = properties.Get<FirstPersonLayerPropertyData>();

            InitalizeDropdown(firstPersonData.MovementID);
        }

        private void InitalizeDropdown(int selectedID)
        {
            MovementModusSwitcher switcher = ServiceLocator.GetService<FirstPersonViewer>().MovementSwitcher;

            int optionIndex = 0;
            List<string> moveOptions = new List<string>();
            for (int i = 0; i < switcher.MovementPresets.Count; i++)
            {
                ViewerState modus = switcher.MovementPresets[i];

                if (modus.id == selectedID) optionIndex = i;
                moveOptions.Add(modus.viewName);
            }

            modusDropdown.AddOptions(moveOptions);
            modusDropdown.SetValueWithoutNotify(optionIndex);
            modusDropdown.onValueChanged.AddListener(OnMovementModeChanged);

            RefreshSettings(switcher.MovementPresets[optionIndex]);
        }

        private void OnMovementModeChanged(int index)
        {
            MovementModusSwitcher switcher = ServiceLocator.GetService<FirstPersonViewer>().MovementSwitcher;
            ViewerState state = switcher.MovementPresets[index];

            firstPersonData.SetMovementID(state.id);
            firstPersonData.settingValues.Clear();
            RefreshSettings(state);
        }

        private void RefreshSettings(ViewerState viewerState)
        {
            ClearSettings();

            foreach (ViewerSetting setting in viewerState.editableSettings.list)
            {
                if (!setting.isVisible) continue;
                if (setting is not ViewerSettingValue) continue;

                CreateSettingsUI(setting);
            }
        }

        private void CreateSettingsUI(ViewerSetting setting)
        {
            string settingKey = setting.GetSettingName();

            //Gets a prefab based on the settings class name.
            ViewerSettingComponentInput componentprefab = componentList.settingPrefabs.First(s => s.className == setting.GetType().Name).prefab as ViewerSettingComponentInput;
            ViewerSettingComponentInput settingObject = Instantiate(componentprefab, settingParent);
            settingObject.Init(setting);

            if (!firstPersonData.settingValues.ContainsKey(settingKey))
                firstPersonData.settingValues.Add(settingKey, setting.GetDefaultValue());

            settingObject.SetValue(firstPersonData.settingValues[settingKey]);
            settingObject.SetPropertyData(firstPersonData);
        }

        private void ClearSettings()
        {
            foreach (var (setting, callback) in addedCallbacks)
            {
                if (setting != null) setting.movementSetting.OnValueChanged.RemoveListener(callback);
            }
            addedCallbacks.Clear();

            foreach (Transform child in settingParent)
            {
                Destroy(child.gameObject);
            }
        }

        private void OnDestroy()
        {
            ClearSettings();
        }
    }
}
