using System.Collections;
using System.Collections.Generic;
using System.Windows.Forms;
using RuntimeHandle;
using UnityEngine;
using UnityEngine.UI;

namespace Netherlands3D.Twin
{
    public class TransformHandleButtonsPanel : MonoBehaviour
    {
        [SerializeField] private RectTransform buttonsPanel;

        [SerializeField] private ToggleGroupItem positionToggle;
        [SerializeField] private ToggleGroupItem rotationToggle;
        [SerializeField] private ToggleGroupItem scaleToggle; 

        private void Awake() {
            buttonsPanel.gameObject.SetActive(false);
        }

        public void ShowPanel(bool show)
        {
            buttonsPanel.gameObject.SetActive(show);
        }

        public void SetLocks(TransformAxes transformLocks)
        {
            Debug.Log("SetLocks", transformLocks.gameObject);

            //Check if axis are locked
            positionToggle.SetInteractable(!transformLocks.PositionLocked);
            rotationToggle.SetInteractable(!transformLocks.RotationLocked);
            scaleToggle.SetInteractable(!transformLocks.ScaleLocked);

            //If current toggle is enabled but is locked, pick another one
            PickAvailableTransform();
        }

        private void PickAvailableTransform()
        {
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

        public void ClearLocks()
        {
            positionToggle.SetInteractable(true);
            rotationToggle.SetInteractable(true);
            scaleToggle.SetInteractable(true);
        }
    }
}
