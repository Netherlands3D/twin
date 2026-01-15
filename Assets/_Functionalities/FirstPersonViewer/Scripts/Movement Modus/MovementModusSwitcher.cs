using Netherlands3D.FirstPersonViewer.ViewModus;
using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;

namespace Netherlands3D.FirstPersonViewer
{
    public class MovementModusSwitcher : MonoBehaviour
    {
        private FirstPersonViewerInput input;

        [Header("Movement")]
        [field: SerializeField] public List<ViewerState> MovementPresets { private set; get; }
        public ViewerState CurrentMovement { private set; get; }

        public event Action<ViewerState> OnMovementPresetChanged;

        private ViewerState setViewerState;
        private Dictionary<string, object> setViewerSettings;

        public void SetViewerInput(FirstPersonViewerInput input)
        {
            this.input = input;
        }

        private void Update()
        {
            if (input.CyclePreviousModus.triggered) ChangeViewerModus(-1);
            else if (input.CycleNextModus.triggered) ChangeViewerModus(1);
        }

        public void ChangeViewerModus(int switchDirection)
        {
            if (input.IsInputfieldSelected()) return;

            int currentIndex = MovementPresets.IndexOf(CurrentMovement);

            currentIndex += switchDirection % MovementPresets.Count + MovementPresets.Count;
            currentIndex %= MovementPresets.Count;

            LoadMovementPreset(MovementPresets[currentIndex]);
        }

        public void LoadMovementPreset(ViewerState viewerState, Dictionary<string, object> settings = null)
        {
            CurrentMovement = viewerState;
            
            foreach (ViewerSetting setting in CurrentMovement.editableSettings.list)
            {
                setting.InvokeOnValueChanged(setting.GetDefaultValue());
            }

            //Load override settings if provided.
            if (settings != null)
            {
                foreach (KeyValuePair<string, object> setting in settings)
                {
                    ViewerSetting viewerSetting = CurrentMovement.editableSettings.list.FirstOrDefault(s => s.GetSettingName() == setting.Key);
                    if (viewerSetting != null) viewerSetting.InvokeOnValueChanged(setting.Value);
                }
            }

            //Send events
            OnMovementPresetChanged?.Invoke(CurrentMovement);
        }

        public void LoadMovementPreset(int index) => LoadMovementPreset(MovementPresets[index]);


        public void SetViewer(ViewerState viewerState, Dictionary<string, object> settings)
        {
            if (viewerState == null) viewerState = MovementPresets[0];

            setViewerState = viewerState;
            setViewerSettings = settings;
        }

        public void ApplyViewer() => LoadMovementPreset(setViewerState, setViewerSettings);

        private void OnDestroy()
        {
            MovementPresets.ForEach(m => m.Uninitialize());
        }
    }
}
