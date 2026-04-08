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
                input.OnLockStateChanged.AddListener(OnLockStateChanged);
            }
            else
            {
                input.OnLockStateChanged.RemoveListener(OnLockStateChanged);
            }
        }

        private void OnLockStateChanged(bool locked)
        {
            //Don't show the fake cursos when not locking the mouse the center.
            if (!ServiceLocator.GetService<FirstPersonViewer>().Input.GetMouseLockModus()) return;

            gameObject.SetActive(locked);
        }
    }
}
