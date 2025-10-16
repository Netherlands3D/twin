using Netherlands3D.FirstPersonViewer.Events;
using Netherlands3D.FirstPersonViewer.UI;
using Netherlands3D.FirstPersonViewer.ViewModus;
using Netherlands3D.Twin.UI.Tooltips;
using System;
using System.Collections.Generic;
using System.Windows.Forms;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Netherlands3D.FirstPersonViewer.UI
{
    public class MovementModusButton : MonoBehaviour
    {
        private MovementPresets movementPreset;
        private ViewerSettings viewerSettings;

        [SerializeField] private List<Image> movementIcons;

        [Header("Button State")]
        [SerializeField] private GameObject regular;
        [SerializeField] private GameObject selected;

        public void Init(MovementPresets preset, ViewerSettings viewerSettings)
        {
            movementPreset = preset;
            this.viewerSettings = viewerSettings;

            movementIcons.ForEach(i => i.sprite = preset.viewIcon);

            GetComponent<TooltipTrigger>().TooltipText = preset.viewName;
        }

        private void OnEnable()
        {
            ViewerEvents.OnMovementPresetChanged += MovementChanged;
        }

        private void OnDisable()
        {
            ViewerEvents.OnMovementPresetChanged -= MovementChanged;    
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
            //movementSwitcher.LoadMoveModus(movementPreset);
            //movementSwitcher.SetMovementVisible();
        }
    }
}
