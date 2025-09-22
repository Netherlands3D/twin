using Netherlands3D.FirstPersonViewer.Events;
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
            //Not a big fan of this
            viewheightInput.text = FirstPersonViewerData.Instance.ViewHeight.ToString();
            fieldOfViewInput.text = FirstPersonViewerData.Instance.FOV.ToString();
            speedInput.text = FirstPersonViewerData.Instance.Speed.ToString();

            ViewerEvents.OnViewheightChanged += OnViewHeightChanged;
            ViewerEvents.OnFOVChanged += OnFOVChanged;   
            ViewerEvents.OnSpeedChanged += OnSpeedChanged;
        }

        private void OnDisable()
        {
            ViewerEvents.OnViewheightChanged -= OnViewHeightChanged;
            ViewerEvents.OnFOVChanged -= OnFOVChanged;
            ViewerEvents.OnSpeedChanged -= OnSpeedChanged;
        }

        public void ViewHeighEdited(string height) => ViewerEvents.OnViewheightChanged?.Invoke(float.Parse(height));
        
        public void FOVEdited(string fov) => ViewerEvents.OnFOVChanged?.Invoke(float.Parse(fov));
        
        public void SpeedEdited(string speed) => ViewerEvents.OnSpeedChanged?.Invoke(float.Parse(speed));

        private void OnViewHeightChanged(float newHeight) => viewheightInput.text = newHeight.ToString();

        private void OnFOVChanged(float newFOV) => fieldOfViewInput.text = newFOV.ToString();

        private void OnSpeedChanged(float newSpeed) => speedInput.text = newSpeed.ToString();
    }
}
