using GG.Extensions;
using Netherlands3D.FirstPersonViewer.Events;
using Netherlands3D.FirstPersonViewer.Temp;
using Netherlands3D.FirstPersonViewer.ViewModus;
using Netherlands3D.Twin.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Netherlands3D.FirstPersonViewer.UI
{
    public class ViewerSettings : MonoBehaviour
    {
        [SerializeField] private ContentFitterRefresh contentFitterRefresh;

        [Header("Settings Label")]
        [SerializeField] private List<ViewerSettingPrefab> settingPrefabs;
        [SerializeField] private Transform settingParent;

        [Header("Movement Button")]
        [SerializeField] private MovementCollection movementPresets;
        [SerializeField] private MovementModusButton movementModusButtonPrefab;
        [SerializeField] private Transform movementParent;


        [Header("Temp")]
        [SerializeField] private MovementPresets tempMovement;

        private void Start()
        {
            CreateMovementPresetButtons(movementPresets);
        }

        private void OnEnable()
        {
            RefreshSettings(tempMovement);
            ViewerEvents.OnMovementPresetChanged += RefreshSettings;
        }

        private void OnDisable()
        {
            ViewerEvents.OnMovementPresetChanged -= RefreshSettings;
        }

        private void CreateMovementPresetButtons(MovementCollection movements)
        {
            foreach (MovementPresets preset in movements.presets)
            {
                MovementModusButton moveButtonObj = Instantiate(movementModusButtonPrefab, movementParent);
                moveButtonObj.Init(preset, this);
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

            StartCoroutine(Test());
        }

        private IEnumerator Test()
        {
            //yield return null;
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
