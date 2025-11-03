using Netherlands3D.FirstPersonViewer.ViewModus;
using UnityEngine;
using System.Collections.Generic;
using System;

namespace Netherlands3D.FirstPersonViewer
{
    public class MovementModusSwitcher : MonoBehaviour
    {
        private FirstPersonViewerInput input;

        [Header("Movement")]
        [field: SerializeField] public List<ViewerState> MovementPresets { private set; get; }
        public ViewerState CurrentMovement { private set; get; }

        public event Action<ViewerState> OnMovementPresetChanged;

        private void Awake()
        {
            input = GetComponent<FirstPersonViewerInput>();
        }

        private void Update()
        {
            if (input.CyclePreviousModus.triggered) ChangeViewerModus(-1);
            else if (input.CycleNextModus.triggered) ChangeViewerModus(1);        
        }

        public void ChangeViewerModus(int switchDirection)
        {
            if (FirstPersonViewerInput.IsInputfieldSelected()) return;

            int currentIndex = MovementPresets.IndexOf(CurrentMovement);

            currentIndex += switchDirection % MovementPresets.Count + MovementPresets.Count;
            currentIndex %= MovementPresets.Count;

            LoadMovementPreset(MovementPresets[currentIndex]);
        }

        public void LoadMovementPreset(ViewerState viewerState)
        {
            CurrentMovement = viewerState;

            //Send events
            OnMovementPresetChanged?.Invoke(CurrentMovement);

            //$$ TODO Only supports floats for now should be revisited
            foreach (ViewerSetting setting in CurrentMovement.editableSettings.list)
            {
                setting.InvokeOnValueChanged(setting.GetValue());
            }
        }

        public void LoadMovementPreset(int index) => LoadMovementPreset(MovementPresets[index]);
    }
}
