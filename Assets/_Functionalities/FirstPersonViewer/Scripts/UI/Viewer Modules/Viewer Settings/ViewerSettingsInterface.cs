using Netherlands3D.FirstPersonViewer.ViewModus;
using Netherlands3D.Services;
using Netherlands3D.Twin.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Netherlands3D.FirstPersonViewer.UI
{
    public class ViewerSettingsInterface : MonoBehaviour
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
            modusSwitcher = ServiceLocator.GetService<MovementModusSwitcher>();

            CreateMovementPresetButtons(modusSwitcher.MovementPresets, modusSwitcher.CurrentMovement);
            RefreshSettings(modusSwitcher.CurrentMovement);

            ServiceLocator.GetService<MovementModusSwitcher>().OnMovementPresetChanged += RefreshSettings;
        }

        private void OnDisable()
        {
            ServiceLocator.GetService<MovementModusSwitcher>().OnMovementPresetChanged -= RefreshSettings;
        }

        private void CreateMovementPresetButtons(List<ViewerState> viewerStates, ViewerState activeState)
        {
            foreach (Transform child in settingParent)
            {
                Destroy(child.gameObject);
            }

            foreach (ViewerState state in viewerStates)
            {
                MovementModusButton moveButtonObj = Instantiate(movementModusButtonPrefab, movementParent);
                moveButtonObj.Init(state, this);
                if (activeState == state) moveButtonObj.SetSelected(true);
            }
        }

        private void RefreshSettings(ViewerState viewerState)
        {
            foreach(Transform child in settingParent)
            {
                Destroy(child.gameObject);
            }

            foreach (ViewerSetting setting in viewerState.editableSettings.list)
            {
                if (!setting.isVisible) continue;

                //Gets a prefab based on the settings class name.
                ViewerSettingComponent componentprefab = settingPrefabs.First(s => s.className == setting.GetType().Name).prefab;

                ViewerSettingComponent settingObject = Instantiate(componentprefab, settingParent);
                settingObject.Init(setting);
            }

            StartCoroutine(UpdateCanvas());
        }

        public void ModusButtonPressed(ViewerState preset)
        {
            modusSwitcher.LoadMovementPreset(preset);
        }

        //TODO Use Vertical/Horizontal Layout group instead of content size fitters
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
