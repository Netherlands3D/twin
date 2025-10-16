using Netherlands3D.FirstPersonViewer.ViewModus;
using Netherlands3D.FirstPersonViewer.Events;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System;
using Netherlands3D.FirstPersonViewer.Temp;
using Netherlands3D.Events;

namespace Netherlands3D.FirstPersonViewer.UI
{
    [Obsolete("Will be removed and moved to ViewerSettings.cs (When the toolbar rework will come.")]
    public class MovementModusSwitcher : MonoBehaviour
    {
        [Header("Input")]
        [SerializeField] private InputActionAsset inputMap;
        private InputAction cycleNextAction;
        private InputAction cyclePreviousAction;

        [Header("Movement")]
        [SerializeField] private MovementCollection movementPresets;
        private MovementPresets currentMovement;

        private void Awake()
        {
            ViewerEvents.OnViewerSetupComplete += ViewerEnterd;
        }

        private void Start()
        {
            cycleNextAction = inputMap.FindAction("NavigateModusNext");
            cyclePreviousAction = inputMap.FindAction("NavigateModusPrevious");
        }

        private void OnDestroy()
        {
            ViewerEvents.OnViewerSetupComplete -= ViewerEnterd;
        }

        private void ViewerEnterd()
        {
            LoadMoveModus(0);
        }

        private void Update()
        {
            if (cyclePreviousAction.triggered) ChangeViewerModus(-1);
            else if (cycleNextAction.triggered) ChangeViewerModus(1);        
        }

        public void ChangeViewerModus(int switchDirection)
        {
            if (FirstPersonViewerInput.IsInputfieldSelected()) return;

            int currentIndex = movementPresets.presets.IndexOf(currentMovement) + switchDirection;

            if (currentIndex < 0) currentIndex = movementPresets.presets.Count - 1;
            else if (currentIndex >= movementPresets.presets.Count) currentIndex = 0;

            LoadMoveModus(currentIndex);
        }

        private void LoadMoveModus(int index)
        {
            if (index >= movementPresets.presets.Count || index < 0) return;

            currentMovement = movementPresets.presets[index];

            int nextIndex = index + 1;
            if (nextIndex >= movementPresets.presets.Count) nextIndex = 0;



            int prevIndex = index - 1;
            if (prevIndex < 0) prevIndex = movementPresets.presets.Count - 1;

            

            //Send events
            ViewerEvents.OnMovementPresetChanged?.Invoke(currentMovement);

            //$$ Only supports floats for now should be revisited
            foreach (ViewerSetting setting in currentMovement.editableSettings.list)
            {
                ViewerSettingsEvents<float>.Invoke(setting.settingName, (float)setting.GetValue());
            }
        }

        public void LoadMoveModus(MovementPresets movePresets) => LoadMoveModus(movementPresets.presets.IndexOf(movePresets));
    }
}
