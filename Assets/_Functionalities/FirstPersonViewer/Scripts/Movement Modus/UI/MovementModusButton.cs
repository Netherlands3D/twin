using Netherlands3D.FirstPersonViewer.ViewModus;
using Netherlands3D.Services;
using Netherlands3D.Twin.UI.Tooltips;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Netherlands3D.FirstPersonViewer.UI
{
    public class MovementModusButton : MonoBehaviour
    {
        private MovementPresets movementPreset;
        private ViewerSettingsInterface viewerSettings;

        [SerializeField] private List<Image> movementIcons;

        [Header("Button State")]
        [SerializeField] private GameObject regular;
        [SerializeField] private GameObject selected;

        public void Init(MovementPresets preset, ViewerSettingsInterface viewerSettings)
        {
            movementPreset = preset;
            this.viewerSettings = viewerSettings;

            movementIcons.ForEach(i => i.sprite = preset.viewIcon);

            GetComponent<TooltipTrigger>().TooltipText = preset.viewName;
        }

        private void OnEnable()
        {
            ServiceLocator.GetService<MovementModusSwitcher>().OnMovementPresetChanged += MovementChanged;
        }

        private void OnDisable()
        {
            ServiceLocator.GetService<MovementModusSwitcher>().OnMovementPresetChanged -= MovementChanged;    
        }
        private void MovementChanged(MovementPresets presets)
        {
            SetSelected(presets == movementPreset);
        }

        public void SetSelected(bool enabled)
        {
            regular.SetActive(!enabled);
            selected.SetActive(enabled);
        }

        public void ButtonClicked()
        {
            viewerSettings.ModusButtonPressed(movementPreset);
        }
    }
}
