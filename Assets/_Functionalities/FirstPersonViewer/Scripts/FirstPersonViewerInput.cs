using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using Netherlands3D.SelectionTools;
using System;
using UnityEngine.Events;
using UnityEngine.InputSystem.Controls;

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
        public Vector2 LookDelta { private set; get; }
        public InputAction ExitInput { private set; get; }
        public InputAction LeftClick { private set; get; }
        public InputAction ResetInput { private set; get; }

        public InputAction CycleNextModus { private set; get; }
        public InputAction CyclePreviousModus { private set; get; }

        private InputAction exitModifier;

        [Header("Exit")]
        [SerializeField] private float exitDuration = 1;
        [SerializeField] private float exitViewDelay = .15f;
        private float exitTimer;
        public UnityEvent<string> onShowSnackbarExit;
        [SerializeField] private string fpvExitText;

        private bool isEditingInputfield;
        private GameObject selectedUI;
        private List<MonoBehaviour> inputLocks;

        //Mouse Locking
        public bool LockInput => inputLocks.Count > 0;
        public bool LockCamera { private set; get; }
        private bool isLocked;

        //Events
        public event Action<float> ExitDuration;
        public event Action<bool> OnLockStateChanged;
        private event Action<bool> OnInputExit;

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
            isLocked = true;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            onShowSnackbarExit.Invoke(fpvExitText);
        }

        private void OnDisable()
        {
            inputActionAsset.Disable();
        }

        private void Update()
        {
            HandleCursorLocking();

            isEditingInputfield = IsInputfieldSelected();

            //HandleExiting();

            LookDelta = LookInput.ReadValue<Vector2>();
            TouchInput();
        }

        private void HandleCursorLocking()
        {
            //When editing an inputfield just block this function.
            if (isEditingInputfield) return;

            //When key is released release/lock mouse
            if (ExitInput.WasReleasedThisFrame())
            {
                ToggleCursor(isLocked);
            }
            else if (LeftClick.triggered && !Interface.PointerIsOverUI())
            {
                //When no UI object is detected lock the mouse to screen again, Lock Cursor.
                ToggleCursor(false);
            }

        }

        private void ToggleCursor(bool unlock)
        {
            // Lock the mouse cursor to the screen using the old method to keep it centered (used by the Object Selector).
            if (unlock) AddInputLockConstrain(this);
            else RemoveInputLockConstrain(this);

            Cursor.lockState = unlock ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = unlock;
            if (!unlock) onShowSnackbarExit.Invoke(fpvExitText);

            isLocked = !unlock;
            LockCamera = unlock;

            OnLockStateChanged?.Invoke(isLocked);
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
                    ExitDuration?.Invoke(percentageTime);
                }

                if (exitTimer == 0)
                {
                    ExitDuration?.Invoke(-1);
                    OnInputExit?.Invoke(exitModifier.IsPressed());
                }
            }
            else if (ExitInput.WasReleasedThisFrame()) ExitDuration?.Invoke(-1); //Reset the visual
            else exitTimer = exitDuration;
        }

        public void ViewerExited()
        {
            //TODO Move this to a application wide cursor manager.
            ToggleCursor(true);
        }

        public void AddInputLockConstrain(MonoBehaviour monoBehaviour) => inputLocks.Add(monoBehaviour);

        public void RemoveInputLockConstrain(MonoBehaviour monoBehaviour) => inputLocks.Remove(monoBehaviour);

        public void SetExitCallback(Action<bool> callback) => OnInputExit = callback;

        public bool IsInputfieldSelected()
        {
            GameObject selected = EventSystem.current.currentSelectedGameObject;

            if (selected == null)
            {
                selectedUI = null;
                return false;
            }

            if (selected == selectedUI) return isEditingInputfield;

            selectedUI = selected;
            return selected.GetComponent<TMP_InputField>() != null;
        }

        private void TouchInput()
        {
            if (Touchscreen.current == null) return;

            foreach (TouchControl touch in Touchscreen.current.touches)
            {
                if (!touch.press.isPressed) continue;

                int id = touch.touchId.ReadValue();

                if (EventSystem.current.IsPointerOverGameObject(id))
                    continue;

                Vector2 delta = touch.delta.ReadValue();
                LookDelta = delta;
            }
        }
    }
}
