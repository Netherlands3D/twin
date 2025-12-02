using Netherlands3D.Services;
using UnityEngine;

namespace Netherlands3D.FirstPersonViewer.UI
{
    public class FPVCrosshairVisibility : MonoBehaviour
    {
        private FirstPersonViewerInput input;

        private void Start()
        {
            input = ServiceLocator.GetService<FirstPersonViewer>().Input;
        }

        public void EnableCursor(bool enable)
        {
            
            if (enable)
            {
                input.OnLockStateChanged += OnLockStateChanged;
            }
            else
            {
                input.OnLockStateChanged -= OnLockStateChanged;
            }
        }

        private void OnLockStateChanged(bool locked)
        {
            //Don't show the fake cursos when not locking the mouse the center.
            if (!input.GetMouseLockModus()) return;

            gameObject.SetActive(locked);
        }
    }
}
