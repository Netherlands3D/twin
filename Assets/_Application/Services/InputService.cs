using UnityEngine;
using UnityEngine.InputSystem;

namespace Netherlands3D.Services
{
    public class InputService : MonoBehaviour
    {
        [Header("InputPolygon")]
        [SerializeField] private InputActionAsset inputPolygonActionAsset;
        private InputActionMap polygonSelectionActionMap;
        
        public InputAction PolygonPointerAction => polygonSelectionActionMap.FindAction("Point");
        public InputAction PolygonTapAction => polygonSelectionActionMap.FindAction("Tap");
        public InputAction PolygonEscapeAction => polygonSelectionActionMap.FindAction("Escape");
        public InputAction PolygonFinishAction => polygonSelectionActionMap.FindAction("Finish");
        public InputAction PolygonTapSecondaryAction => polygonSelectionActionMap.FindAction("TapSecondary");
        public InputAction PolygonModifierAction => polygonSelectionActionMap.FindAction("Modifier");
        public InputAction PolygonClickAction => polygonSelectionActionMap.FindAction("Click");

        void Awake()
        {
            polygonSelectionActionMap = inputPolygonActionAsset.FindActionMap("PolygonSelection");
            if (!polygonSelectionActionMap.enabled)
            {
                Debug.LogWarning("polygonSelectionActionMap was not enabled, but assigned as the input action map. Enabling the input action map", this);
                polygonSelectionActionMap.Enable();
            }
        }
    }
}
