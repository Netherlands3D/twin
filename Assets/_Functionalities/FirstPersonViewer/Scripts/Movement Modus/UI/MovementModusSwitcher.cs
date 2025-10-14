using Netherlands3D.FirstPersonViewer.ViewModus;
using Netherlands3D.FirstPersonViewer.Events;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System;

namespace Netherlands3D.FirstPersonViewer.UI
{
    [Obsolete("Will be removed and moved to ViewerSettings.cs (When the toolbar rework will come.")]
    public class MovementModusSwitcher : MonoBehaviour
    {
        [Header("Input")]
        [SerializeField] private InputActionAsset inputMap;
        private InputAction cycleNextAction;
        private InputAction cyclePreviousAction;

        [Header("UI")]
        [SerializeField] private Image currentMovemodeImage;
        [SerializeField] private Image nextMovemodeImage;
        [SerializeField] private Image prevMovemodeImage;
        [SerializeField] private MovementModusButton movementButtonPrefab;
        [SerializeField] private Transform movementButtonParent;

        [Header("Movement")]
        [SerializeField] private MovementCollection movementPresets;
        private MovementPresets currentMovement;

        private void Awake()
        {
            ViewerEvents.OnViewerEntered += ViewerEnterd;
        }

        private void Start()
        {
            cycleNextAction = inputMap.FindAction("NavigateModusNext");
            cyclePreviousAction = inputMap.FindAction("NavigateModusPrevious");

            movementPresets.presets.ForEach(preset =>
            {
                MovementModusButton tempButton = Instantiate(movementButtonPrefab, movementButtonParent);
                tempButton.SetupButton(preset, this);
            });
        }

        private void OnDestroy()
        {
            ViewerEvents.OnViewerEntered -= ViewerEnterd;
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
            currentMovemodeImage.sprite = currentMovement.viewIcon;

            int nextIndex = index + 1;
            if (nextIndex >= movementPresets.presets.Count) nextIndex = 0;

            nextMovemodeImage.sprite = movementPresets.presets[nextIndex].viewIcon;

            int prevIndex = index - 1;
            if (prevIndex < 0) prevIndex = movementPresets.presets.Count - 1;

            prevMovemodeImage.sprite = movementPresets.presets[prevIndex].viewIcon;

            //Send events
            ViewerEvents.OnMovementPresetChanged?.Invoke(currentMovement);
            //ViewerEvents.OnViewheightChanged?.Invoke(currentMovement.viewHeight);
            //ViewerEvents.OnFOVChanged?.Invoke(currentMovement.fieldOfView);
            //ViewerEvents.OnSpeedChanged?.Invoke(currentMovement.speedInKm);
        }

        public void LoadMoveModus(MovementPresets movePresets) => LoadMoveModus(movementPresets.presets.IndexOf(movePresets));

        public void SetMovementVisible()
        {
            movementButtonParent.gameObject.SetActive(!movementButtonParent.gameObject.activeSelf);
        }
    }
}
