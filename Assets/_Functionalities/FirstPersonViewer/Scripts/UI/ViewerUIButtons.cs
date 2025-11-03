using Netherlands3D.Services;
using UnityEngine;


namespace Netherlands3D.FirstPersonViewer.UI
{
    public class ViewerUIButtons : MonoBehaviour
    {
        public void ExitViewer() => FirstPersonViewer.OnViewerExited?.Invoke(false);
        public void ResetToStart() => ServiceLocator.GetService<FirstPersonViewer>().OnResetToGround?.Invoke();
        public void SnapToGround() => ServiceLocator.GetService<FirstPersonViewer>().OnResetToGround?.Invoke();
    }
}