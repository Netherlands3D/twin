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
        
        private InputAction openProjectAction;
        private InputAction saveProjectAction;
        private InputAction undoAction;
        private InputAction redoAction;
        
        private InputActionMap polygonSelectionActionMap;
        
        private InputAction polygonPointerAction;
        private InputAction polygonTapAction;
        private InputAction polygonEscapeAction;
        private InputAction polygonFinishAction;
        private InputAction polygonTapSecondaryAction;
        private InputAction polygonModifierAction;
        private InputAction polygonClickAction;

        public InputAction PolygonPointerAction => polygonPointerAction ??= polygonSelectionActionMap.FindAction("Point");
        public InputAction PolygonTapAction => polygonTapAction ??= polygonSelectionActionMap.FindAction("Tap");
        public InputAction PolygonEscapeAction => polygonEscapeAction ??= polygonSelectionActionMap.FindAction("Escape");
        public InputAction PolygonFinishAction => polygonFinishAction ??= polygonSelectionActionMap.FindAction("Finish");
        public InputAction PolygonTapSecondaryAction => polygonTapSecondaryAction ??= polygonSelectionActionMap.FindAction("TapSecondary");
        public InputAction PolygonModifierAction => polygonModifierAction ??= polygonSelectionActionMap.FindAction("Modifier");
        public InputAction PolygonClickAction => polygonClickAction ??= polygonSelectionActionMap.FindAction("Click");
        
        public InputAction OpenProjectAction => openProjectAction ??= inputPolygonActionAsset.FindAction("Projects/Open");
        public InputAction SaveProjectAction => saveProjectAction ??= inputPolygonActionAsset.FindAction("Projects/Save");

        void Awake()
        {
            polygonSelectionActionMap = inputPolygonActionAsset.FindActionMap("PolygonSelection");
            if (!polygonSelectionActionMap.enabled)
            {
                Debug.LogWarning("polygonSelectionActionMap was not enabled, but assigned as the input action map. Enabling the input action map", this);
                polygonSelectionActionMap.Enable();
            }
        }

        private void OnEnable()
        {
            openProjectAction.Enable();
            saveProjectAction.Enable();
        }

        private void OnDisable()
        {
            openProjectAction.Disable();
            saveProjectAction.Disable();
        }
    }
}
