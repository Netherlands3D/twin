using RuntimeHandle;
using UnityEngine;


namespace Netherlands3D.Twin
{
    public class TransformHandleButtonsPanel : MonoBehaviour
    {
        [SerializeField] private RectTransform buttonsPanel;

        [SerializeField] private ToggleGroupItem positionToggle;
        [SerializeField] private ToggleGroupItem rotationToggle;
        [SerializeField] private ToggleGroupItem scaleToggle; 

        public TransformHandleInterfaceToggle TransformHandleInterfaceToggle { get; set; }
        private TransformAxes transformLocks;

        private void Awake() {
            buttonsPanel.gameObject.SetActive(false);

            positionToggle.Toggle.onValueChanged.AddListener((toggled) => UpdateGizmoHandles());
            rotationToggle.Toggle.onValueChanged.AddListener((toggled) => UpdateGizmoHandles());
            scaleToggle.Toggle.onValueChanged.AddListener((toggled) => UpdateGizmoHandles());
        }

        public void ShowPanel(bool show)
        {
            buttonsPanel.gameObject.SetActive(show);
        }

        public void SetLocks(TransformAxes transformLocks)
        {
            this.transformLocks = transformLocks;

            //Check if axis are locked
            positionToggle.SetInteractable(!transformLocks.PositionLocked);
            rotationToggle.SetInteractable(!transformLocks.RotationLocked);
            scaleToggle.SetInteractable(!transformLocks.ScaleLocked);

            //If current toggle is enabled but is locked, pick another one
            PickAvailableTransform();
        }

        public void UpdateGizmoHandles()
        {
            if(!transformLocks) return;

            if (positionToggle.Toggle.isOn)
                TransformHandleInterfaceToggle.RuntimeTransformHandle.SetAxis(transformLocks.positionAxes);
            else if (rotationToggle.Toggle.isOn)
                TransformHandleInterfaceToggle.RuntimeTransformHandle.SetAxis(transformLocks.rotationAxes);
            else if (scaleToggle.Toggle.isOn)
                TransformHandleInterfaceToggle.RuntimeTransformHandle.SetAxis(transformLocks.scaleAxes);
        }
 
        public void ClearLocks()
        {
            transformLocks = null;

            positionToggle.SetInteractable(true);
            rotationToggle.SetInteractable(true);
            scaleToggle.SetInteractable(true);
        }

        private void PickAvailableTransform()
        {
            //If we are set to manipulate a tramsform property axis, pick another available that is allowed
            if (!positionToggle.IsInteractable && positionToggle.Toggle.isOn)
            {
                if (rotationToggle.IsInteractable)
                    rotationToggle.Toggle.isOn = true;
                else if (scaleToggle.IsInteractable)
                    scaleToggle.Toggle.isOn = true;
            }
            else if (!rotationToggle.IsInteractable && rotationToggle.Toggle.isOn)
            {
                if (positionToggle.IsInteractable)
                    positionToggle.Toggle.isOn = true;
                else if (scaleToggle.IsInteractable)
                    scaleToggle.Toggle.isOn = true;
            }
            else if (!scaleToggle.IsInteractable && scaleToggle.Toggle.isOn)
            {
                if (positionToggle.IsInteractable)
                    positionToggle.Toggle.isOn = true;
                else if (rotationToggle.IsInteractable)
                    rotationToggle.Toggle.isOn = true;
            }
        }
    }
}
