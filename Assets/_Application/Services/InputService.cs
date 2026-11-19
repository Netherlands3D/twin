using System;
using Netherlands3D.Twin;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using EditableNameField = Netherlands3D.UI.Components.EditableNameField;

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
        private InputAction exitAction;
        private InputAction toggleDashboardAction;
        
        private InputActionMap polygonSelectionActionMap;
        private InputActionMap cameraInputActionMap;
        
        private InputAction polygonPointerAction;
        private InputAction polygonTapAction;
        private InputAction polygonEscapeAction;
        private InputAction polygonFinishAction;
        private InputAction polygonTapSecondaryAction;
        private InputAction polygonModifierAction;
        private InputAction polygonClickAction;

        private InputAction leftClickAction, rightClickAction, leftClickUpAction, rightClickUpAction, longPressAction, touchAction;
        
        public InputAction PolygonPointerAction => polygonPointerAction ??= polygonSelectionActionMap.FindAction("Point");
        public InputAction PolygonTapAction => polygonTapAction ??= polygonSelectionActionMap.FindAction("Tap");
        public InputAction PolygonEscapeAction => polygonEscapeAction ??= polygonSelectionActionMap.FindAction("Escape");
        public InputAction PolygonFinishAction => polygonFinishAction ??= polygonSelectionActionMap.FindAction("Finish");
        public InputAction PolygonTapSecondaryAction => polygonTapSecondaryAction ??= polygonSelectionActionMap.FindAction("TapSecondary");
        public InputAction PolygonModifierAction => polygonModifierAction ??= polygonSelectionActionMap.FindAction("Modifier");
        public InputAction PolygonClickAction => polygonClickAction ??= polygonSelectionActionMap.FindAction("Click");
        
        public InputAction OpenProjectAction => openProjectAction ??= applicationActionMap.FindAction("Projects/Open");
        public InputAction SaveProjectAction => saveProjectAction ??= applicationActionMap.FindAction("Projects/Save");
        public InputAction ExitAction => exitAction ??= applicationActionMap.FindAction("Projects/Exit");
        public InputAction ToggleDashboardAction => toggleDashboardAction ??= applicationActionMap.FindAction("Dashboard/ToggleFunctionality", true);
        
        public InputAction LeftClickAction => leftClickAction ??= cameraInputActionMap.FindAction("LeftClick");
        public InputAction RightClickAction => rightClickAction ??= cameraInputActionMap.FindAction("RightClick");
        public InputAction LeftClickUpAction => leftClickUpAction ??= cameraInputActionMap.FindAction("LeftClickUp");
        public InputAction RightClickUpAction => rightClickUpAction ??= cameraInputActionMap.FindAction("RightClickUp");
        
        public InputAction LongPressAction => longPressAction ??= cameraInputActionMap.FindAction("LongPress");
        public InputAction TouchAction => touchAction ??= cameraInputActionMap.FindAction("Touch");

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
            ExitAction.Enable();
            ToggleDashboardAction.Enable();
        }

        private void OnDisable()
        {
            OpenProjectAction.Disable();
            SaveProjectAction.Disable();
            ExitAction.Disable();
            ToggleDashboardAction.Disable();
        }

        public bool IsInputFieldSelected()
        {
            var root = App.UIRoot?.Root;
            if (root == null) return false;

            var focusedElement = root.focusController?.focusedElement as VisualElement;
            for (var element = focusedElement; element != null; element = element.parent)
            {
                for (var type = element.GetType(); type != null; type = type.BaseType)
                {
                    if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(TextInputBaseField<>))
                        return true;
                }
            }

            return root.Query<EditableNameField>().ToList().Exists(IsVisibleNameFieldBeingEdited);
        }

        private static bool IsVisibleNameFieldBeingEdited(EditableNameField field)
        {
            // Inline name fields can stay in edit mode after losing focus while Shift is held.
            if (!field.IsEditing || !field.visible) return false;

            for (VisualElement element = field; element != null; element = element.parent)
            {
                if (element.resolvedStyle.display == DisplayStyle.None)
                    return false;
            }

            return true;
        }

        public void SetCameraActionsEnabled(bool enable)
        {
            if (enable)
                cameraInputActionMap.Enable();
            else 
                cameraInputActionMap.Disable();
        }
    }
}
