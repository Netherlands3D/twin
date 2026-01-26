using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Netherlands3D.Twin.Cameras
{
    public class CameraService : MonoBehaviour
    {
        [SerializeField] private List<Camera> cameraObjects;
        
        public UnityEvent<Camera> OnSwitchCamera = new();

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

        private Camera current;
        private Camera previous;
        
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

        public void SwitchCamera(Camera cameraObject)
        {
            if (cameraObject == null)
            {
                Debug.LogError("CameraSwitcher: cameraObject is null!");
                return;
            }
           
            //Save previous
            previous = current;
            current = cameraObject;
            previous.gameObject.SetActive(false);
            cameraObject.gameObject.SetActive(true);
            
            OnSwitchCamera.Invoke(cameraObject);
        }

        public void SwitchToPreviousCamera()
        {
            if (previous != null)
            {
                SwitchCamera(previous);
            }
        }
    }
}
