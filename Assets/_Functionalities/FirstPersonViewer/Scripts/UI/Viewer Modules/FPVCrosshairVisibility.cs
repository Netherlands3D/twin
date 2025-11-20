using Netherlands3D.Services;
using UnityEngine;

namespace Netherlands3D.FirstPersonViewer.UI
{
    public class FPVCrosshairVisibility : MonoBehaviour
    {
        public void EnableCursor(bool enable)
        {
            FirstPersonViewerInput input = ServiceLocator.GetService<FirstPersonViewer>().Input;
            
            if (enable)
            {
                input.OnLockStateChanged += OnLockStateChanged;
            }
            else
            {
                input.OnLockStateChanged -= OnLockStateChanged;
            }
        }

        private void OnLockStateChanged(CursorLockMode locked)
        {
            gameObject.SetActive(locked == CursorLockMode.Locked);
        }
    }
}
