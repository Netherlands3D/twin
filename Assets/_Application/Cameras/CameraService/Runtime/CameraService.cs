using System.Collections.Generic;
using NetTopologySuite.Geometries;
using UnityEngine;
using UnityEngine.Events;

namespace Netherlands3D.Twin.Cameras
{
    public class CameraService : MonoBehaviour
    {
        [SerializeField] private List<Camera> cameraObjects;
        public UnityEvent<Vector3> OnPositionChanged;
        public UnityEvent<Camera> OnSwitchCamera = new();
        
        private Vector3 lastPosition;

        public const float CAMERA_MAXHEIGHT = 1500;

        public Camera ActiveCamera
        {
            get
            {
                if (current == null)
                {
                    if (cameraObjects[0] == null || cameraObjects.Count == 0)
                    {
                        if (cameraObjects.Count == 0)
                        {
                            Debug.LogError("please assign cameras to the cameraobjects");
                            cameraObjects.Add(Camera.main);
                        }
                        cameraObjects[0] = Camera.main;
                    }
                    current = cameraObjects[0];
                }
                return current;
            }
        }

        public Camera PreviousCamera => previous;

        public bool IsCurrentCameraFpv => isCurrentCameraFPV;

        private Camera current;
        private Camera previous;
        private bool isCurrentCameraFPV = false;
        
        private void Start()
        {
            if (cameraObjects != null && cameraObjects.Count > 0)
            {
                foreach(Camera c in cameraObjects)
                    if(c != cameraObjects[0])
                        c.gameObject.SetActive(false);
                SwitchCamera(ActiveCamera);
            }
        }

        public void SwitchCamera(Camera cameraObject, bool isFPV = false)
        {
            if (cameraObject == null)
            {
                Debug.LogError("CameraSwitcher: cameraObject is null!");
                return;
            }
           
            //Save previous
            previous = current;
            current = cameraObject;
            previous?.gameObject.SetActive(false);
            cameraObject.gameObject.SetActive(true);
            isCurrentCameraFPV = isFPV;
            OnSwitchCamera.Invoke(cameraObject);
        }

        public void SwitchToPreviousCamera()
        {
            if (previous != null)
            {
                SwitchCamera(previous);
            }
        }
        
        private void LateUpdate()
        {
            if (ActiveCamera.transform.position == lastPosition) return;
            PositionChanged();
        }
        
        private void PositionChanged()
        {
            OnPositionChanged.Invoke(ActiveCamera.transform.position);
            lastPosition = ActiveCamera.transform.position;
        }
    }
}
