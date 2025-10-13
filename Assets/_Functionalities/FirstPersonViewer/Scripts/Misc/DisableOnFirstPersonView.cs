using Netherlands3D.FirstPersonViewer.Events;
using UnityEngine;
using UnityEngine.Events;

namespace Netherlands3D.FirstPersonViewer.Miscellaneous
{
    public class DisableOnFirstPersonView : MonoBehaviour
    {
        [SerializeField] private UnityEvent onViewerEnterd = new UnityEvent();
        [SerializeField] private UnityEvent onViewerExited = new UnityEvent();

        private void Start()
        {
            ViewerEvents.OnViewerEntered += ViewerEnterd;
            ViewerEvents.OnViewerExited += ViewerExited;
        }

        private void OnDestroy()
        {
            ViewerEvents.OnViewerEntered -= ViewerEnterd;
            ViewerEvents.OnViewerExited -= ViewerExited;
        }

        private void ViewerEnterd() => onViewerEnterd.Invoke();
        private void ViewerExited(bool modified) => onViewerExited.Invoke();
    }
}
