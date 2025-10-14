using GG.Extensions;
using Netherlands3D.FirstPersonViewer.Events;
using Netherlands3D.FirstPersonViewer.Temp;
using Netherlands3D.FirstPersonViewer.ViewModus;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

namespace Netherlands3D.FirstPersonViewer.UI
{
    public class ViewerSettings : MonoBehaviour
    {
        [SerializeField] private List<ViewerSettingPrefab> settingPrefabs;

        [SerializeField] private Transform settingParent;

        [SerializeField] private MovementPresets tempMovement;

        private void OnEnable()
        {
            RefreshSettings(tempMovement);
            //ViewerEvents.OnMovementPresetChanged += 
        }





        private void RefreshSettings(MovementPresets movementPreset)
        {
            foreach (ViewerSetting setting in movementPreset.editableSettings.list)
            {
                if (!setting.isVisible) continue;

                ViewerSettingComponent Componentprefab = settingPrefabs.First(s => s.className == setting.GetType().Name).prefab;

                ViewerSettingComponent settingObject = Instantiate(Componentprefab, settingParent);
                settingObject.Init(setting);
            }
        }
    }

    [System.Serializable]
    public class ViewerSettingPrefab
    {
        public string className;
        public ViewerSettingComponent prefab;
    }
}
