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
            FirstPersonViewer.OnViewerEntered += ViewerEnterd;
            FirstPersonViewer.OnViewerExited += ViewerExited;
        }

        private void OnDestroy()
        {
            FirstPersonViewer.OnViewerEntered -= ViewerEnterd;
            FirstPersonViewer.OnViewerExited -= ViewerExited;
        }

        private void ViewerEnterd() => onViewerEnterd.Invoke();
        private void ViewerExited() => onViewerExited.Invoke();
    }
}
