using RuntimeHandle;
using UnityEngine;

namespace Netherlands3D.Twin.UI
{
    public class TransformHandleInterfaceToggle : MonoBehaviour
    {
        [SerializeField] private RuntimeTransformHandle runtimeTransformHandle;
        [SerializeField] private TransformHandleButtonsPanel handleButtonsPanel;

        public RuntimeTransformHandle RuntimeTransformHandle { get => runtimeTransformHandle; private set => runtimeTransformHandle = value; }

        private void Awake() 
        {
            RuntimeTransformHandle = GetComponent<RuntimeTransformHandle>();
            handleButtonsPanel.TransformHandleInterfaceToggle = this;
        }

        public void SetTransformTarget(GameObject targetGameObject)
        {
            handleButtonsPanel.ShowPanel(true);
            handleButtonsPanel.ShowVisibilityPanel(true);

            //Set the target of the transform handle
            RuntimeTransformHandle.SetTarget(targetGameObject);

            //Check if specific Transform axes locks are set
            if(targetGameObject.TryGetComponent(out TransformAxes transformLocks))
            {
                handleButtonsPanel.SetLocks(transformLocks);
            }
            else
            {
                handleButtonsPanel.ClearLocks();
                RuntimeTransformHandle.SetAxis(HandleAxes.XYZ);
            }

            handleButtonsPanel.UpdateGizmoHandles();
        }

        public void ClearTransformTarget()
        {
            gameObject.SetActive(false);
            handleButtonsPanel.ShowPanel(false);
            handleButtonsPanel.ShowVisibilityPanel(false);
        }

        public void ShowVisibilityPanel(bool show)
        {
            handleButtonsPanel.ShowVisibilityPanel(show);
        }
    }
}
