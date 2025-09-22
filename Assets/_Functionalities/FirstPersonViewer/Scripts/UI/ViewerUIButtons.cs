using Netherlands3D.FirstPersonViewer.Events;
using UnityEngine;

namespace Netherlands3D.FirstPersonViewer.UI
{
    public class ViewerUIButtons : MonoBehaviour
    {
        public void ResetToStart() => ViewerEvents.OnResetToStart?.Invoke();
    }
}
