using Netherlands3D.FirstPersonViewer.Events;
using System.Collections.Generic;
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

        [Header("Exit")]
        [SerializeField] private float exitDuration = .75f;
        private float exitTimer;

        private List<MonoBehaviour> cameraLocks;
        public bool LockCamera => cameraLocks.Count > 0;

        private void Awake()
        {
            MoveAction = inputActionAsset.FindAction("Move");
            SprintAction = inputActionAsset.FindAction("Sprint");
            JumpAction = inputActionAsset.FindAction("Jump");
            VerticalMoveAction = inputActionAsset.FindAction("VerticalMove");
            LookInput = inputActionAsset.FindAction("Look");
            ExitInput = inputActionAsset.FindAction("Exit");
            LeftClick = inputActionAsset.FindAction("LClick");

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            ViewerEvents.OnViewerExited += ViewerExited;
        }

        private void OnEnable()
        {
            cameraLocks = new List<MonoBehaviour>();
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
            if (ExitInput.triggered)
            {
                if(Cursor.lockState == CursorLockMode.Locked)
                {
                    AddCameraLockConstrain(this);
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                } else
                {
                    RemoveCameraLockConstrain(this);
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false; 
                }
            } else if (LeftClick.triggered)
            {
                if(!IsPointerOverUIObject())
                {
                    RemoveCameraLockConstrain(this);
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                }
            }

            if (ExitInput.IsPressed())
            {
                exitTimer = Mathf.Max(exitTimer - Time.deltaTime, 0);

                if (exitTimer == 0) ViewerEvents.OnViewerExited?.Invoke();
            }
            else exitTimer = exitDuration;
        }

        private void ViewerExited()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            Destroy(gameObject);
        }

        public void AddCameraLockConstrain(MonoBehaviour monoBehaviour) => cameraLocks.Add(monoBehaviour);

        public void RemoveCameraLockConstrain(MonoBehaviour monoBehaviour) => cameraLocks.Remove(monoBehaviour);

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
