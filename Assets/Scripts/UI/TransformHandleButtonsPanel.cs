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

        [SerializeField] private Toggle positionToggle;
        [SerializeField] private Toggle rotationToggle;
        [SerializeField] private Toggle scaleToggle; 

        [Header("Icon colors")]
        [SerializeField] private Color enabledIconColor = Color.white;
        [SerializeField] private Color disabledIconColor = Color.grey;

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
            positionToggle.interactable = !transformLocks.PositionLocked;
            rotationToggle.interactable = !transformLocks.RotationLocked;
            scaleToggle.interactable = !transformLocks.ScaleLocked;

            //Set color
            positionToggle.image.color = transformLocks.PositionLocked ? disabledIconColor : enabledIconColor;
            rotationToggle.image.color = transformLocks.RotationLocked ? disabledIconColor : enabledIconColor;
            scaleToggle.image.color = transformLocks.ScaleLocked ? disabledIconColor : enabledIconColor;
        }

        public void ClearLocks()
        {
            positionToggle.interactable = true;
            rotationToggle.interactable = true;
            scaleToggle.interactable = true;
        }
    }
}
