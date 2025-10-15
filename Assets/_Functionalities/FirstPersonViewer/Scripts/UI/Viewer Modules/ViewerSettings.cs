using Netherlands3D.FirstPersonViewer.Events;
using Netherlands3D.Services;
using TMPro;
using UnityEngine;

namespace Netherlands3D.FirstPersonViewer.UI
{
    public class ViewerSettings : MonoBehaviour
    {
        [SerializeField] private TMP_InputField viewheightInput;
        [SerializeField] private TMP_InputField fieldOfViewInput;
        [SerializeField] private TMP_InputField speedInput;

        private void OnEnable()
        {
            ViewerEvents.OnViewheightChanged += OnViewHeightChanged;
            ViewerEvents.OnFOVChanged += OnFOVChanged;   
            ViewerEvents.OnSpeedChanged += OnSpeedChanged;
        }

        private void Start()
        {
            FirstPersonViewerData viewerData = ServiceLocator.GetService<FirstPersonViewerData>();

            //Not a big fan of this
            viewheightInput.text = viewerData.ViewHeight.ToString();
            fieldOfViewInput.text = viewerData.FOV.ToString();
            speedInput.text = viewerData.Speed.ToString();
        }

        private void OnDisable()
        {
            ViewerEvents.OnViewheightChanged -= OnViewHeightChanged;
            ViewerEvents.OnFOVChanged -= OnFOVChanged;
            ViewerEvents.OnSpeedChanged -= OnSpeedChanged;
        }

        //Doesn't reset the previous value, but won't fix this because I'm already working on a better settings menu.
        public void ViewHeighEdited(string height)
        {
            if (!float.TryParse(height, out float value)) return;

            ViewerEvents.OnViewheightChanged?.Invoke(value);
        }

        public void FOVEdited(string fov)
        {
            if(!float.TryParse(fov, out float value)) return; 

            ViewerEvents.OnFOVChanged?.Invoke(value);
        }

        public void SpeedEdited(string speed)
        {
            if(!float.TryParse(speed, out float value)) return;

            ViewerEvents.OnSpeedChanged?.Invoke(value);
        }

        private void OnViewHeightChanged(float newHeight) => viewheightInput.text = newHeight.ToString();

        private void OnFOVChanged(float newFOV) => fieldOfViewInput.text = newFOV.ToString();

        private void OnSpeedChanged(float newSpeed) => speedInput.text = newSpeed.ToString();
    }
}
