using GG.Extensions;
using Netherlands3D.Services;
using UnityEngine;
using UnityEngine.UI;

namespace Netherlands3D.FirstPersonViewer.UI
{
    public class FPVSettingsUI : MonoBehaviour
    {
        [SerializeField] private Toggle mouseLockingToggle;
        [SerializeField] private Slider sensitivitySlider;

        private void OnEnable()
        {
            FirstPersonViewer firstPersonViewer = ServiceLocator.GetService<FirstPersonViewer>();
            sensitivitySlider.SetValue(firstPersonViewer.FirstPersonCamera.GetSensitivity());
            //Invoke this so the text display will update.
            sensitivitySlider.onValueChanged?.Invoke(firstPersonViewer.FirstPersonCamera.GetSensitivity());

            mouseLockingToggle.SetValue(firstPersonViewer.Input.GetMouseLockModus());
        }

        public void OnSensitivityChanged(float sensitivity)
        {
            ServiceLocator.GetService<FirstPersonViewer>().FirstPersonCamera.SetSensitivity(sensitivity);
        }

        public void OnMouseLockModeChanged(bool useMouseLocking)
        {
            ServiceLocator.GetService<FirstPersonViewer>().Input.SetMouseLockModus(useMouseLocking);
        }
    }
}
