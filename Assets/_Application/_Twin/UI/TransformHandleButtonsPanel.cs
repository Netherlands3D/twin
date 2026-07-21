// using RuntimeHandle;
// using UnityEngine;
// using UnityEngine.UI;
//
// namespace Netherlands3D.Twin.UI
// {
//     public class TransformHandleButtonsPanel : MonoBehaviour
//     {
//         [SerializeField] private RectTransform buttonsPanel;
//         //[SerializeField] private RectTransform visibilityPanel;
//         [SerializeField] private ToggleGroupItem positionToggle;
//         [SerializeField] private ToggleGroupItem rotationToggle;
//         [SerializeField] private ToggleGroupItem scaleToggle;
//         [SerializeField] private Button snapButton;
//         public TransformHandleInterfaceToggle TransformHandleInterfaceToggle { get; set; }
//        
//
//         private void OnEnable()
//         {
//             positionToggle.Toggle.onValueChanged.AddListener(UpdateGizmoHandles);
//             rotationToggle.Toggle.onValueChanged.AddListener(UpdateGizmoHandles);
//             scaleToggle.Toggle.onValueChanged.AddListener(UpdateGizmoHandles);
//             snapButton.onClick.AddListener(SnapObject);           
//         }
//         
//         private void OnDisable()
//         {
//             positionToggle.Toggle.onValueChanged.RemoveListener(UpdateGizmoHandles);
//             rotationToggle.Toggle.onValueChanged.RemoveListener(UpdateGizmoHandles);
//             scaleToggle.Toggle.onValueChanged.RemoveListener(UpdateGizmoHandles);
//             snapButton.onClick.RemoveListener(SnapObject);
//         }
//     }
// }
