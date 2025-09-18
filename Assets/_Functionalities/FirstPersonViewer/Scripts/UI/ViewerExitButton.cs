using Netherlands3D.FirstPersonViewer.Events;
using UnityEngine;

namespace Netherlands3D.FirstPersonViewer.UI
{
    public class ViewerExitButton : MonoBehaviour
    {
        public void ExitViewer()
        {
            ViewerEvents.OnViewerExited?.Invoke();
        }
    }
}
