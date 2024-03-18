using RuntimeHandle;
using UnityEngine;

namespace Netherlands3D.Twin
{
    public class TransformHandleInterfaceToggle : MonoBehaviour
    {
        [SerializeField] private RuntimeTransformHandle runtimeTransformHandle;
        [SerializeField] private TransformHandleButtonsPanel handleButtonsPanel;

        private void Awake() {
            runtimeTransformHandle = GetComponent<RuntimeTransformHandle>();
        }

        public void SetTransformTarget(GameObject targetGameObject)
        {
            //Set the target of the transform handle
            runtimeTransformHandle.SetTarget(targetGameObject);

            //Check if specific Transform axes locks are set
            if(targetGameObject.TryGetComponent(out TransformAxes transformLocks))
            {
                handleButtonsPanel.SetLocks(transformLocks);
                runtimeTransformHandle.SetAxis(transformLocks.positionAxes);
            }
            else
            {
                handleButtonsPanel.ClearLocks();
                runtimeTransformHandle.SetAxis(HandleAxes.XYZ);
            }
        }

        public void ClearTransformTarget()
        {
            gameObject.SetActive(false);
        }

        private void OnDisable() {
            handleButtonsPanel.ShowPanel(false);
        }

        private void OnEnable() {
            handleButtonsPanel.ShowPanel(true);
        }
    }
}
