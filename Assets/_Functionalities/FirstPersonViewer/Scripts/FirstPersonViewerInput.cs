using Netherlands3D.FirstPersonViewer.Events;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using Netherlands3D.SelectionTools;

namespace Netherlands3D.FirstPersonViewer
{

    public class FirstPersonViewerInput : MonoBehaviour
    {
        [Header("Input")]
        [SerializeField] private InputActionAsset inputActionAsset;

        public InputAction MoveAction { private set; get; }
        public InputAction SprintAction { private set; get; }
        public InputAction JumpAction { private set; get; }
        public InputAction VerticalMoveAction { private set; get; }
        public InputAction LookInput { private set; get; }
        public InputAction ExitInput { private set; get; }
        public InputAction LeftClick { private set; get; }
        public InputAction HideUI { private set; get; }
        public InputAction ResetInput { private set; get; }

        public InputAction CycleNextModus { private set; get; }
        public InputAction CyclePreviousModus { private set; get; }

        [Header("Exit")]
        [SerializeField] private float exitDuration = 1;
        [SerializeField] private float exitViewDelay = .15f;
        private float exitTimer;

        private bool isEditingInputfield;
        private List<MonoBehaviour> inputLocks;
        public bool LockInput => inputLocks.Count > 0;

        private void Awake()
        {
            MoveAction = inputActionAsset.FindAction("Move");
            SprintAction = inputActionAsset.FindAction("Sprint");
            JumpAction = inputActionAsset.FindAction("Jump");
            VerticalMoveAction = inputActionAsset.FindAction("VerticalMove");
            LookInput = inputActionAsset.FindAction("Look");
            ExitInput = inputActionAsset.FindAction("Exit");
            LeftClick = inputActionAsset.FindAction("LClick");
            HideUI = inputActionAsset.FindAction("HideUI");
            ResetInput = inputActionAsset.FindAction("Reset");
            CycleNextModus = inputActionAsset.FindAction("NavigateModusNext");
            CyclePreviousModus = inputActionAsset.FindAction("NavigateModusPrevious");

            inputLocks = new List<MonoBehaviour>();

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            ViewerEvents.OnViewerExited += ViewerExited;
        }

        private void OnEnable()
        {
            inputActionAsset.Enable();
        }

        private void OnDisable()
        {
            inputActionAsset.Disable();
        }

        private void OnDestroy()
        {
            ViewerEvents.OnViewerExited -= ViewerExited;
        }

        private void Update()
        {
            HandleCursorLocking();

            HandleExiting();
            
            if (HideUI.triggered) ViewerEvents.OnHideUI?.Invoke();
        }

        private void HandleCursorLocking()
        {
            //When editing an inputfield just block this function.
            if (ShouldSkipCursorLocking()) return;

            //When key is released release/lock mouse
            if (ExitInput.WasReleasedThisFrame() && !isEditingInputfield)
            {
                ToggleCursorLock();
            }
            else if (LeftClick.triggered && !Interface.PointerIsOverUI()) 
            {
                //When no UI object is detected lock the mouse to screen again.
                LockCursor();
            }
        }

        private bool ShouldSkipCursorLocking()
        {
            if (!ExitInput.triggered) return false;

            isEditingInputfield = IsInputfieldSelected();
            return isEditingInputfield;
        }

        private void ToggleCursorLock()
        {
            if (Cursor.lockState == CursorLockMode.Locked)
            {
                AddInputLockConstrain(this);
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else
            {
                RemoveInputLockConstrain(this);
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        private void LockCursor()
        {
            RemoveInputLockConstrain(this);
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        //When holding the exit key and not editing any inputfield. Start the exiting proceidure. 
        private void HandleExiting()
        {
            if (ExitInput.IsPressed() && !isEditingInputfield)
            {
                exitTimer = Mathf.Max(exitTimer - Time.deltaTime, 0);

                //Delay x seconds before showing the progress. So the UI component isn't flickering
                if (exitTimer < exitDuration - exitViewDelay)
                {
                    float percentageTime = Mathf.Clamp01(1f - ((exitTimer + exitViewDelay) / exitDuration));
                    ViewerEvents.ExitDuration?.Invoke(percentageTime);
                }

                if (exitTimer == 0)
                {
                    ViewerEvents.ExitDuration?.Invoke(-1);
                    ViewerEvents.OnViewerExited?.Invoke();
                }
            }
            else if (ExitInput.WasReleasedThisFrame()) ViewerEvents.ExitDuration?.Invoke(-1); //Reset the visual
            else exitTimer = exitDuration;
        }

        private void ViewerExited()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            Destroy(gameObject);
        }

        public void AddInputLockConstrain(MonoBehaviour monoBehaviour) => inputLocks.Add(monoBehaviour);

        public void RemoveInputLockConstrain(MonoBehaviour monoBehaviour) => inputLocks.Remove(monoBehaviour);

        public static bool IsInputfieldSelected()
        {
            GameObject selected = EventSystem.current.currentSelectedGameObject;

            if (selected == null) return false;

            return selected.GetComponent<TMP_InputField>() != null;
        }
    }
}
