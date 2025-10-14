using Netherlands3D.FirstPersonViewer.Events;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

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
            if (ExitInput.triggered)
            {
                isEditingInputfield = IsInputfieldSelected();
                if (isEditingInputfield) return;
            }

            if (ExitInput.WasReleasedThisFrame() && !isEditingInputfield)
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

                ViewerEvents.OnMouseStateChanged?.Invoke(Cursor.lockState);
            }
            else if (LeftClick.triggered)
            {
                if (!IsPointerOverUIObject())
                {
                    RemoveInputLockConstrain(this);
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                    ViewerEvents.OnMouseStateChanged?.Invoke(Cursor.lockState);
                }
            }
        }

        private void HandleExiting()
        {
            if (ExitInput.IsPressed() && !isEditingInputfield)
            {
                exitTimer = Mathf.Max(exitTimer - Time.deltaTime, 0);

                //Wait .delay x seconds before showing the progress.
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
            else if (ExitInput.WasReleasedThisFrame()) ViewerEvents.ExitDuration?.Invoke(-1);
            else exitTimer = exitDuration;
        }

        private void ViewerExited()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            //Delay by one frame to prevent error.
            Destroy(gameObject, Time.deltaTime);
        }

        public void AddInputLockConstrain(MonoBehaviour monoBehaviour) => inputLocks.Add(monoBehaviour);

        public void RemoveInputLockConstrain(MonoBehaviour monoBehaviour) => inputLocks.Remove(monoBehaviour);

        public static bool IsInputfieldSelected()
        {
            GameObject selected = EventSystem.current.currentSelectedGameObject;

            if (selected == null) return false;

            return selected.GetComponent<TMP_InputField>() != null;
        }

        //Kinda slow
        public static bool IsPointerOverUIObject()
        {
            PointerEventData eventDataCurrentPosition = new PointerEventData(EventSystem.current);
            eventDataCurrentPosition.position = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventDataCurrentPosition, results);
            return results.Count > 1; //Idk there seems to be an invisble ui element somewhere.
        }
    }
}
