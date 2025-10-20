using Netherlands3D.FirstPersonViewer.Events;
using Netherlands3D.FirstPersonViewer.ViewModus;
using Netherlands3D.Services;
using Netherlands3D.Twin.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Netherlands3D.FirstPersonViewer.UI
{
    public class ViewerSettings : MonoBehaviour
    {
        [SerializeField] private ContentFitterRefresh contentFitterRefresh;

        [Header("Settings Label")]
        [SerializeField] private List<ViewerSettingPrefab> settingPrefabs;
        [SerializeField] private Transform settingParent;

        [Header("Movement Button")]
        [SerializeField] private MovementModusButton movementModusButtonPrefab;
        [SerializeField] private Transform movementParent;

        private MovementModusSwitcher modusSwitcher; 

        private void OnEnable()
        {
            modusSwitcher = ServiceLocator.GetService<FirstPersonViewerData>().ModusSwitcher;

            CreateMovementPresetButtons(modusSwitcher.MovementPresets, modusSwitcher.CurrentMovement);
            RefreshSettings(modusSwitcher.CurrentMovement);

            ViewerEvents.OnMovementPresetChanged += RefreshSettings;
        }

        private void OnDisable()
        {
            ViewerEvents.OnMovementPresetChanged -= RefreshSettings;
        }

        private void CreateMovementPresetButtons(List<MovementPresets> movementPresets, MovementPresets activePreset)
        {
            foreach (Transform child in settingParent)
            {
                Destroy(child.gameObject);
            }

            foreach (MovementPresets preset in movementPresets)
            {
                MovementModusButton moveButtonObj = Instantiate(movementModusButtonPrefab, movementParent);
                moveButtonObj.Init(preset, this);
                if (activePreset == preset) moveButtonObj.SetSelected(true);
            }
        }

        private void RefreshSettings(MovementPresets movementPreset)
        {
            foreach(Transform child in settingParent)
            {
                Destroy(child.gameObject);
            }

            foreach (ViewerSetting setting in movementPreset.editableSettings.list)
            {
                if (!setting.isVisible) continue;

                ViewerSettingComponent Componentprefab = settingPrefabs.First(s => s.className == setting.GetType().Name).prefab;

                ViewerSettingComponent settingObject = Instantiate(Componentprefab, settingParent);
                settingObject.Init(setting);
            }

            StartCoroutine(UpdateCanvas());
        }

        public void ModusButtonPressed(MovementPresets preset)
        {
            modusSwitcher.LoadMovementPreset(preset);
        }

        private IEnumerator UpdateCanvas()
        {
            Canvas.ForceUpdateCanvases();
            contentFitterRefresh.RefreshContentFitters();
            yield return null;
            contentFitterRefresh.RefreshContentFitters();
        }
    }

    [System.Serializable]
    public class ViewerSettingPrefab
    {
        public string className;
        public ViewerSettingComponent prefab;
    }
}
