using Netherlands3D.FirstPersonViewer.ViewModus;
using Netherlands3D.FirstPersonViewer.Events;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System;
using Netherlands3D.FirstPersonViewer.Temp;
using Netherlands3D.Events;
using System.Collections.Generic;

namespace Netherlands3D.FirstPersonViewer
{
    public class MovementModusSwitcher : MonoBehaviour
    {
        private FirstPersonViewerInput input;

        [Header("Movement")]
        [field: SerializeField] public List<MovementPresets> MovementPresets { private set; get; }
        public MovementPresets CurrentMovement { private set; get; }

        private void Awake()
        {
            input = GetComponent<FirstPersonViewerInput>();
            
            ViewerEvents.OnViewerSetupComplete += ViewerEnterd;
        }

        private void OnDestroy()
        {
            ViewerEvents.OnViewerSetupComplete -= ViewerEnterd;
        }

        private void ViewerEnterd()
        {
            LoadMovementPreset(MovementPresets[0]);
        }

        private void Update()
        {
            if (input.CyclePreviousModus.triggered) ChangeViewerModus(-1);
            else if (input.CycleNextModus.triggered) ChangeViewerModus(1);        
        }

        public void ChangeViewerModus(int switchDirection)
        {
            if (FirstPersonViewerInput.IsInputfieldSelected()) return;

            int currentIndex = MovementPresets.IndexOf(CurrentMovement) + switchDirection;

            if (currentIndex < 0) currentIndex = MovementPresets.Count - 1;
            else if (currentIndex >= MovementPresets.Count) currentIndex = 0;

            LoadMovementPreset(MovementPresets[currentIndex]);
        }

        public void LoadMovementPreset(MovementPresets movePresets)
        {
            CurrentMovement = movePresets;

            //Send events
            ViewerEvents.OnMovementPresetChanged?.Invoke(CurrentMovement);

            //$$ Only supports floats for now should be revisited
            foreach (ViewerSetting setting in CurrentMovement.editableSettings.list)
            {
                ViewerSettingsEvents<float>.Invoke(setting.settingsLabel, (float)setting.GetValue());
                ViewerEvents.onSettingChanged?.Invoke(setting.settingsLabel.settingName, setting.GetValue());
            }
        }

        private void LoadMoveModus(int index)
        {
            if (index >= MovementPresets.Count || index < 0) return;

            CurrentMovement = MovementPresets[index];

            //int nextIndex = index + 1;
            //if (nextIndex >= movementPresets.Count) nextIndex = 0;



            //int prevIndex = index - 1;
            //if (prevIndex < 0) prevIndex = movementPresets.Count - 1;
        }
    }
}
