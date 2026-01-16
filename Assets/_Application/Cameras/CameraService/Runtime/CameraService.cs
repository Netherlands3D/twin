using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Netherlands3D.Twin.Cameras
{
    public class CameraService : MonoBehaviour
    {
        [SerializeField] private List<Camera> cameraObjects;
        
        public UnityEvent<Camera> OnSwitchCamera = new();

        public Camera ActiveCamera => current == null ? cameraObjects[0] : current; //on load this will be null
        public Camera PreviousCamera => previous;

        private Camera current;
        private Camera previous;
        
        private void Start()
        {
            if (cameraObjects != null && cameraObjects.Count > 0)
            {
                SwitchCamera(cameraObjects[0]);
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
            
            foreach(Camera c in cameraObjects)
                if(c != cameraObject)
                    c.gameObject.SetActive(false);

            //Enable new
            cameraObject.gameObject.SetActive(true);
            
            OnSwitchCamera.Invoke(cameraObject);
        }

        // public void SwitchCamera(int index)
        // {
        //     SwitchCamera(cameraObjects[index]);
        // }

        public void SwitchToPreviousCamera()
        {
            if (previous != null)
            {
                SwitchCamera(previous);
            }
        }
    }
}
