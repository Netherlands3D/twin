using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using System;
using Netherlands3D.Twin;
using UnityEngine.Events;
using Cursor = UnityEngine.Cursor;
using Netherlands3D.UI_Toolkit.Scripts;
using UnityEngine.UIElements;

namespace Netherlands3D.FirstPersonViewer
{
    public class FirstPersonViewerInput : MonoBehaviour
    {
        [Header("Input")]
        [SerializeField] private InputActionAsset inputActionAsset;

        public InputAction MoveAction { private set; get; }
        public InputAction SprintAction { private set; get; }
        public InputAction SpaceAction { private set; get; }
        public InputAction VerticalMoveAction { private set; get; }
        public InputAction LookInput { private set; get; }
        public InputAction ExitInput { private set; get; }
        public InputAction LeftClick { private set; get; }
        public InputAction ResetInput { private set; get; }

        public InputAction CycleNextModus { private set; get; }
        public InputAction CyclePreviousModus { private set; get; }

        private InputAction exitModifier;

        [Header("Exit")]
        [SerializeField] private float exitDuration = 1;

        private float exitTimer;
        [SerializeField] private string fpvExitText;

        private bool isEditingInputfield;
        private GameObject selectedUI;
        private List<MonoBehaviour> inputLocks;

        //Mouse Locking
        public bool LockInput => inputLocks.Count > 0;
        public bool BlockCameraInput { private set; get; }
        private bool lockMouseModus;
        private bool cursorLocked;
        private bool isActive;

        //Events
        public UnityEvent<float> ExitDuration = new();
        public UnityEvent<bool> OnLockStateChanged = new();
        private Action<bool> OnInputExit; //Callback

        private void Awake()
        {
            MoveAction = inputActionAsset.FindAction("Move");
            SprintAction = inputActionAsset.FindAction("Sprint");
            SpaceAction = inputActionAsset.FindAction("Space");
            VerticalMoveAction = inputActionAsset.FindAction("VerticalMove");
            LookInput = inputActionAsset.FindAction("Look");
            ExitInput = inputActionAsset.FindAction("Exit");
            LeftClick = inputActionAsset.FindAction("LClick");
            ResetInput = inputActionAsset.FindAction("Reset");
            CycleNextModus = inputActionAsset.FindAction("NavigateModusNext");
            CyclePreviousModus = inputActionAsset.FindAction("NavigateModusPrevious");
            exitModifier = inputActionAsset.FindAction("ExitModifier");

            inputLocks = new List<MonoBehaviour>();
        }

        private void OnEnable()
        {
            inputActionAsset.Enable();
        }

        public void OnFPVEnter()
        {
            //Only lock mouse when the locking modus is selected.
            inputLocks.Clear(); // always start clean
            ToggleCursor(lockMouseModus);
        }

        private void OnDisable()
        {
            inputActionAsset.Disable();
        }

        private void Update()
        {
            if (!isActive) return;

            isEditingInputfield = IsInputfieldSelected();
            HandleCursorLocking();

            HandleExiting();
        }

        private void HandleCursorLocking()
        {
            //When editing an inputfield just block this function.
            if (isEditingInputfield) return;

            // click to move mode
            if (!lockMouseModus)
            {
                BlockCameraInput = !LeftClick.IsPressed();
                return;
            }

            // lock cursor mode
            if (ExitInput.WasReleasedThisFrame() && cursorLocked)
            {
                ToggleCursor(false);
                return;
            }

            // Relock only when unlocked, mouse pressed, and not clicking UI.
            if (!cursorLocked && LeftClick.WasPressedThisFrame() && !App.UIRoot.IsPointerOverUI())
            {
                ToggleCursor(true);
            }
        }

        private void ToggleCursor(bool lockCursor)
        {
            // Lock the mouse cursor to the screen using the old method to keep it centered (used by the Object Selector).
            cursorLocked = lockCursor;

            Cursor.lockState = lockCursor
                ? CursorLockMode.Locked
                : CursorLockMode.None;

            Cursor.visible = !lockCursor;

            if (lockCursor)
            {
                RemoveInputLockConstrain(this);
                App.Debug.DisplayMessage(fpvExitText, IconImage.FPV);
            }
            else if (lockMouseModus) // only self-lock in lock-cursor modus
            {
                AddInputLockConstrain(this);
            }

            BlockCameraInput = !lockCursor;
            OnLockStateChanged.Invoke(cursorLocked);
        }

        //When holding the exit key and not editing any inputfield. Start the exiting proceidure. 
        private void HandleExiting()
        {
            if (ExitInput.IsPressed() && !isEditingInputfield)
            {
                exitTimer -= Time.deltaTime;

                float percentageTime = exitTimer / exitDuration;
                ExitDuration.Invoke(percentageTime);
                
                if (exitTimer < 0)
                {
                    OnInputExit.Invoke(exitModifier.IsPressed());
                }
            }
            else if (ExitInput.WasReleasedThisFrame()) ExitDuration.Invoke(-1); //Reset the visual, -1 signifies a non-value for the durationcounter.
            else exitTimer = exitDuration;
        }

        public void ViewerEntered()
        {
            isActive = true;
        }

        public void ViewerExited()
        {
            isActive = false;
            //TODO Move this to a application wide cursor manager.
            ToggleCursor(false);
        }

        public void AddInputLockConstrain(MonoBehaviour monoBehaviour) => inputLocks.Add(monoBehaviour);

        public void RemoveInputLockConstrain(MonoBehaviour monoBehaviour) => inputLocks.Remove(monoBehaviour);

        public void SetExitCallback(Action<bool> callback) => OnInputExit = callback;

        public bool IsInputfieldSelected()
        {
            var focusedElement = App.UIRoot.Root?.focusController?.focusedElement as VisualElement;
            return IsTextInputField(focusedElement);
        }

        private static bool IsTextInputField(VisualElement element)
        {
            var type = element?.GetType();
            while (type != null)
            {
                if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(TextInputBaseField<>))
                {
                    return true;
                }
                type = type.BaseType;
            }

            return false;
        }

        public void SetMouseLockModus(bool lockMouseModus) => this.lockMouseModus = lockMouseModus;
        public bool GetMouseLockModus() => lockMouseModus;
    }
}