using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Netherlands3D.Services
{
    public class InputService : MonoBehaviour
    {
        [Header("InputPolygon")]
        [SerializeField] private InputActionAsset inputPolygonActionAsset;
        [SerializeField] private InputActionAsset applicationActionMap;
        [SerializeField] private InputActionAsset cameraInputActionAsset;
        
        private InputAction openProjectAction;
        private InputAction saveProjectAction;
        private InputAction undoAction;
        private InputAction redoAction;
        
        private InputActionMap polygonSelectionActionMap;
        private InputActionMap cameraInputActionMap;
        
        private InputAction polygonPointerAction;
        private InputAction polygonTapAction;
        private InputAction polygonEscapeAction;
        private InputAction polygonFinishAction;
        private InputAction polygonTapSecondaryAction;
        private InputAction polygonModifierAction;
        private InputAction polygonClickAction;

        private InputAction leftClickAction, rightClickAction, leftClickUpAction, rightClickUpAction;
        
        public InputAction PolygonPointerAction => polygonPointerAction ??= polygonSelectionActionMap.FindAction("Point");
        public InputAction PolygonTapAction => polygonTapAction ??= polygonSelectionActionMap.FindAction("Tap");
        public InputAction PolygonEscapeAction => polygonEscapeAction ??= polygonSelectionActionMap.FindAction("Escape");
        public InputAction PolygonFinishAction => polygonFinishAction ??= polygonSelectionActionMap.FindAction("Finish");
        public InputAction PolygonTapSecondaryAction => polygonTapSecondaryAction ??= polygonSelectionActionMap.FindAction("TapSecondary");
        public InputAction PolygonModifierAction => polygonModifierAction ??= polygonSelectionActionMap.FindAction("Modifier");
        public InputAction PolygonClickAction => polygonClickAction ??= polygonSelectionActionMap.FindAction("Click");
        
        public InputAction OpenProjectAction => openProjectAction ??= applicationActionMap.FindAction("Projects/Open");
        public InputAction SaveProjectAction => saveProjectAction ??= applicationActionMap.FindAction("Projects/Save");
        
        public InputAction LeftClickAction => leftClickAction ??= cameraInputActionMap.FindAction("LeftClick");
        public InputAction RightClickAction => rightClickAction ??= cameraInputActionMap.FindAction("RightClick");
        public InputAction LeftClickUpAction => leftClickUpAction ??= cameraInputActionMap.FindAction("LeftClickUp");
        public InputAction RightClickUpAction => rightClickUpAction ??= cameraInputActionMap.FindAction("RightClickUp");

        void Awake()
        {
            polygonSelectionActionMap = inputPolygonActionAsset.FindActionMap("PolygonSelection");
            if (!polygonSelectionActionMap.enabled)
            {
                Debug.LogWarning("polygonSelectionActionMap was not enabled, but assigned as the input action map. Enabling the input action map", this);
                polygonSelectionActionMap.Enable();
            }
            
            cameraInputActionMap = cameraInputActionAsset.FindActionMap("Camera", true);
            if(!cameraInputActionMap.enabled)
                cameraInputActionMap.Enable();
        }

        private void OnEnable()
        {
            OpenProjectAction.Enable();
            SaveProjectAction.Enable();
        }

        private void OnDisable()
        {
            OpenProjectAction.Disable();
            SaveProjectAction.Disable();
        }
    }
}
