using Netherlands3D.Twin.Cameras.Input;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Netherlands3D.Twin.Configuration.Settings
{
    public class ScrollInputScalerToggle : MonoBehaviour
    {
        private Toggle toggle;

        private void Awake()
        {
            toggle = GetComponent<Toggle>();
            
            var uiScrollScaler = EventSystem.current.GetComponent<ScrollInputScaler>();
            if (uiScrollScaler)
            {
                toggle.isOn = uiScrollScaler.UseZoomScaleValue;
                return;
            }
            var cameraScaler = Camera.main.GetComponent<CameraInputSystemProvider>();
            if (cameraScaler)
            {
                toggle.isOn = cameraScaler.UseZoomScaleValue;
                return;
            }

            Debug.Log("could not find ui scaler or camera scaler, disabling toggle");
            toggle.interactable = false;
        }

        public void ToggleScrollScaler(bool isOn)
        {
            var uiScrollScaler = EventSystem.current.GetComponent<ScrollInputScaler>();
            if (uiScrollScaler)
                uiScrollScaler.UseZoomScaleValue = isOn;

            var cameraScaler = Camera.main.GetComponent<CameraInputSystemProvider>();
            if (cameraScaler)
                cameraScaler.UseZoomScaleValue = isOn;
        }
    }
}