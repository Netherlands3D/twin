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

            float currentSensitivity = firstPersonViewer.FirstPersonCamera.GetSensitivity() * 100;
            sensitivitySlider.SetValue(currentSensitivity);
            //Invoke this so the text display will update.
            sensitivitySlider.onValueChanged?.Invoke(currentSensitivity);

            mouseLockingToggle.SetValue(firstPersonViewer.Input.GetMouseLockModus());
        }

        public void OnSensitivityChanged(float sensitivity)
        {
            ServiceLocator.GetService<FirstPersonViewer>().FirstPersonCamera.SetSensitivity(sensitivity / 100);
        }

        public void OnMouseLockModeChanged(bool useMouseLocking)
        {
            ServiceLocator.GetService<FirstPersonViewer>().Input.SetMouseLockModus(useMouseLocking);
        }
    }
}
