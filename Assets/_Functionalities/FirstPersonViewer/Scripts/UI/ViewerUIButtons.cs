using Netherlands3D.FirstPersonViewer.Events;
using UnityEngine;


namespace Netherlands3D.FirstPersonViewer.UI
{
    public class ViewerUIButtons : MonoBehaviour
    {
        public void ExitViewer() => ViewerEvents.OnViewerExited?.Invoke();
        public void ResetToStart() => ViewerEvents.OnResetToStart?.Invoke();
        public void SnapToGround() => ViewerEvents.OnResetToGround?.Invoke();
        public void HideUI() => ViewerEvents.OnHideUI?.Invoke();
    }
}