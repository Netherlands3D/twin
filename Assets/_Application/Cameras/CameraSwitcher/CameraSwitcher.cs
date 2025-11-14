using System.Collections.Generic;
using UnityEngine;

namespace Netherlands3D.Twin.Cameras
{
    public class CameraSwitcher : MonoBehaviour
    {
        [SerializeField] private List<MonoBehaviour> cameraObjects;

        private CameraState current;
        private CameraState previous;

        [System.Serializable]
        private struct CameraState
        {
            public MonoBehaviour Camera;
            public bool ScriptOnly;
        }

        private void Start()
        {
            if (cameraObjects != null && cameraObjects.Count > 0)
            {
                SwitchCamera(cameraObjects[0], true);
            }
        }

        public void SwitchCamera(MonoBehaviour cameraObject, bool scriptOnly = false)
        {
            if (cameraObject == null)
            {
                Debug.LogError("CameraSwitcher: cameraObject is null!");
                return;
            }

            //Disable current
            if (current.Camera != null)
            {
                SetCameraActive(current, false);
            }

            //Save previous
            previous = current;

            //Enable new
            current = new CameraState { Camera = cameraObject, ScriptOnly = scriptOnly };
            SetCameraActive(current, true);

        }

        public void SwitchCamera(int index, bool scriptOnly = false)
        {
            SwitchCamera(cameraObjects[index], scriptOnly);
        }

        public void SwitchToPreviousCamera()
        {
            if (previous.Camera != null)
            {
                SwitchCamera(previous.Camera, previous.ScriptOnly);
            }
        }

        private void SetCameraActive(CameraState state, bool enable)
        {
            if (state.Camera == null) return;

            if (state.ScriptOnly)
            {
                state.Camera.enabled = enable;
            }
            else
            {
                state.Camera.gameObject.SetActive(enable);
            }
        }
    }
}
