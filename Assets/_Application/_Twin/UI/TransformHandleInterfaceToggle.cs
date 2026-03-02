using RuntimeHandle;
using UnityEngine;
using UnityEngine.Events;

namespace Netherlands3D.Twin.UI
{
    public class TransformHandleInterfaceToggle : MonoBehaviour
    {
        [SerializeField] private RuntimeTransformHandle runtimeTransformHandle;
        [SerializeField] private TransformHandleButtonsPanel handleButtonsPanel;

        private bool enableHandle = true;

        public UnityEvent<GameObject> SetTarget = new();
        public UnityEvent SnapTarget = new();

        public RuntimeTransformHandle RuntimeTransformHandle { get => runtimeTransformHandle; private set => runtimeTransformHandle = value; }

        private void Awake() 
        {
            RuntimeTransformHandle = GetComponent<RuntimeTransformHandle>();
            handleButtonsPanel.TransformHandleInterfaceToggle = this;
        }

        public void SetTransformTarget(GameObject targetGameObject)
        {
            if (!enableHandle) return;

            handleButtonsPanel.ShowPanel(true);
            handleButtonsPanel.ShowVisibilityPanel(false); //enable this if objects need to have the visibility toggle (disabled for now)

            //Set the target of the transform handle
            RuntimeTransformHandle.SetTarget(targetGameObject);
            SetTarget.Invoke(targetGameObject);

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
            SetTarget.Invoke(null);
            gameObject.SetActive(false);
            handleButtonsPanel.ShowPanel(false);
            handleButtonsPanel.ShowVisibilityPanel(false);
        }

        public void ShowVisibilityPanel(bool show)
        {
            handleButtonsPanel.ShowVisibilityPanel(show);
        }

        public void SnapObject()
        {
            SnapTarget.Invoke();
        }

        public void SetTransformHandleEnabled(bool enabled)
        {
            if(!enabled) ClearTransformTarget();
            enableHandle = enabled;
        }
    }
}
