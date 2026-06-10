using RuntimeHandle;
using UnityEngine;
using UnityEngine.UI;

namespace Netherlands3D.Twin.UI
{
    public class TransformHandleButtonsPanel : MonoBehaviour
    {
        [SerializeField] private RectTransform buttonsPanel;
        [SerializeField] private RectTransform visibilityPanel;
        [SerializeField] private ToggleGroupItem positionToggle;
        [SerializeField] private ToggleGroupItem rotationToggle;
        [SerializeField] private ToggleGroupItem scaleToggle;
        [SerializeField] private Button snapButton;
        public TransformHandleInterfaceToggle TransformHandleInterfaceToggle { get; set; }
        private TransformAxes transformLocks;

        private void OnEnable()
        {
            positionToggle.Toggle.onValueChanged.AddListener(UpdateGizmoHandles);
            rotationToggle.Toggle.onValueChanged.AddListener(UpdateGizmoHandles);
            scaleToggle.Toggle.onValueChanged.AddListener(UpdateGizmoHandles);
            snapButton.onClick.AddListener(SnapObject);           
        }
        
        private void OnDisable()
        {
            positionToggle.Toggle.onValueChanged.RemoveListener(UpdateGizmoHandles);
            rotationToggle.Toggle.onValueChanged.RemoveListener(UpdateGizmoHandles);
            scaleToggle.Toggle.onValueChanged.RemoveListener(UpdateGizmoHandles);
            snapButton.onClick.RemoveListener(SnapObject);
        }

        private void Start()
        {
            buttonsPanel.gameObject.SetActive(false);
            visibilityPanel.gameObject.SetActive(false);

        }

        public void ShowPanel(bool show)
        {
            buttonsPanel.gameObject.SetActive(show);
        }

        public void ShowVisibilityPanel(bool show)
        {
            visibilityPanel.gameObject.SetActive(show);
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
        
        private void UpdateGizmoHandles(bool toggled)
        {
            UpdateGizmoHandles();
        }

        public void UpdateGizmoHandles()
        {
            if(!transformLocks) return;

            if (positionToggle.Toggle.isOn)
                TransformHandleInterfaceToggle.RuntimeTransformHandle.SetAxis(ConvertAxis(transformLocks.positionAxes));
            else if (rotationToggle.Toggle.isOn)
                TransformHandleInterfaceToggle.RuntimeTransformHandle.SetAxis(ConvertAxis(transformLocks.rotationAxes));
            else if (scaleToggle.Toggle.isOn)
                TransformHandleInterfaceToggle.RuntimeTransformHandle.SetAxis(ConvertAxis(transformLocks.scaleAxes));
        }
        
        /// <summary>
        /// Convert current axis to match the xyz orientation 
        /// </summary>
        /// <param name="zUpAxis"></param>
        /// <returns></returns>
        private HandleAxes ConvertAxis(HandleAxes zUpAxis)
        {
            //split up the input axis into individual axis components and check if the bit is on
            var xOn = ((int)zUpAxis & (int)HandleAxes.X); 
            var yOn = ((int)zUpAxis & (int)HandleAxes.Y);
            var zOn = ((int)zUpAxis & (int)HandleAxes.Z);

            //move the yBit one to the right to take the z position, and move the zbit one to the left to take the y position.
            yOn >>= 1; 
            zOn <<= 1;
    
            //add the result to recombine the axes
            return (HandleAxes)(xOn + yOn + zOn);
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

        private void SnapObject()
        {
            TransformHandleInterfaceToggle.SnapObject();
        }
    }
}
