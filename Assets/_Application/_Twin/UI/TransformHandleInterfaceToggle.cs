using RuntimeHandle;
using UnityEngine;
using UnityEngine.Events;

namespace Netherlands3D.Twin.UI
{
    public class TransformHandleInterfaceToggle : MonoBehaviour
    {
        [SerializeField] private RuntimeTransformHandle runtimeTransformHandle;
        public UnityEvent OnUpdateGizmoHandles;
      
        private bool enableHandle = true;
        private TransformAxes transformLocks;
        
        private bool positionInteractable;
        public bool PositionInteractable
        {
            get => positionInteractable;
            set
            {
                positionInteractable = value;
                OnUpdateGizmoHandles.Invoke();
            }
        }

        private bool rotationInteractable;
        public bool RotationInteractable
        {
            get => rotationInteractable;
            set
            {
                rotationInteractable = value;
                OnUpdateGizmoHandles.Invoke();
            }
        }

        private bool scaleInteractable;
        public bool ScaleInteractable
        {
            get => scaleInteractable;
            set
            {
                scaleInteractable = value;
                OnUpdateGizmoHandles.Invoke();
            }
        }

        public enum TransformMode
        {
            Position,
            Rotation,
            Scale
        }
        
        private TransformMode currentMode;

        public TransformMode CurrentMode
        {
            get => currentMode;
            set
            {
                currentMode = value;
                runtimeTransformHandle.SetHandleMode((int)currentMode);
                OnUpdateGizmoHandles.Invoke();
            }
        }

        //todo check why return runtimeTransformHandle?.target?.gameobject; crashes
        public GameObject Target
        {
            get
            {
                if (runtimeTransformHandle == null)
                    return null;

                var target = runtimeTransformHandle.target;

                if (target == null)
                    return null;

                return target.gameObject;
            }
        }

        public UnityEvent<GameObject> SetTarget = new();
        public UnityEvent SnapTarget = new();

        public RuntimeTransformHandle RuntimeTransformHandle { get => runtimeTransformHandle; private set => runtimeTransformHandle = value; }

        private void Awake() 
        {
            RuntimeTransformHandle = GetComponent<RuntimeTransformHandle>();
            OnUpdateGizmoHandles.AddListener(UpdateGizmoHandles);
        }

        public void SetTransformTarget(GameObject targetGameObject)
        {
            if (!enableHandle) return;

            //Set the target of the transform handle
            RuntimeTransformHandle.SetTarget(targetGameObject);
            SetTarget.Invoke(targetGameObject);

            //Check if specific Transform axes locks are set
            if(targetGameObject.TryGetComponent(out TransformAxes transformLocks))
            {
                this.transformLocks = transformLocks;

                //Check if axis are locked
                positionInteractable = !transformLocks.PositionLocked;
                rotationInteractable = !transformLocks.RotationLocked;
                scaleInteractable = !transformLocks.ScaleLocked;

                //If current toggle is enabled but is locked, pick another one
                PickAvailableTransform();
            }
            else
            {
                this.transformLocks = null;

                positionInteractable = true;
                rotationInteractable = true;
                scaleInteractable = true;
            
                OnUpdateGizmoHandles.Invoke();
                RuntimeTransformHandle.SetAxis(HandleAxes.XYZ);
            }

            OnUpdateGizmoHandles.Invoke();
        }
        
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

        public void UpdateGizmoHandles()
        {
            if (!transformLocks)
                return;

            switch (currentMode)
            {
                case TransformMode.Position:
                    RuntimeTransformHandle.SetAxis(ConvertAxis(transformLocks.positionAxes));
                    break;

                case TransformMode.Rotation:
                    RuntimeTransformHandle.SetAxis(ConvertAxis(transformLocks.rotationAxes));
                    break;

                case TransformMode.Scale:
                    RuntimeTransformHandle.SetAxis(ConvertAxis(transformLocks.scaleAxes));
                    break;
            }
        }
        
        public void ClearTransformTarget()
        {
            SetTarget.Invoke(null);
            gameObject.SetActive(false);
        }

        public void SnapObject()
        {
            SnapTarget.Invoke();
        }
        
        private void PickAvailableTransform()
        {
            switch (currentMode)
            {
                case TransformMode.Position:
                    if (!positionInteractable)
                    {
                        if (rotationInteractable)
                            CurrentMode = TransformMode.Rotation;
                        else if (scaleInteractable)
                            CurrentMode = TransformMode.Scale;
                    }
                    break;

                case TransformMode.Rotation:
                    if (!rotationInteractable)
                    {
                        if (positionInteractable)
                            CurrentMode = TransformMode.Position;
                        else if (scaleInteractable)
                            CurrentMode = TransformMode.Scale;
                    }
                    break;

                case TransformMode.Scale:
                    if (!scaleInteractable)
                    {
                        if (positionInteractable)
                            CurrentMode = TransformMode.Position;
                        else if (rotationInteractable)
                            CurrentMode = TransformMode.Rotation;
                    }
                    break;
            }
        }

        public void SetTransformHandleEnabled(bool enabled)
        {
            if(!enabled) ClearTransformTarget();
            enableHandle = enabled;
        }
    }
}
