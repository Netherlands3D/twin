using Netherlands3D.Services;
using System;
using UnityEngine;
using UnityEngine.Events;

namespace Netherlands3D.FirstPersonViewer.Miscellaneous
{
    [Obsolete("This script will be replaced with a CameraManager")]
    public class DisableOnFirstPersonView : MonoBehaviour
    {
        [SerializeField] private UnityEvent onViewerEnterd = new UnityEvent();
        [SerializeField] private UnityEvent onViewerExited = new UnityEvent();

        private void Start()
        { 
            FirstPersonViewer fpv = ServiceLocator.GetService<FirstPersonViewer>();
            fpv.OnViewerEntered += ViewerEnterd;
            fpv.OnViewerExited += ViewerExited;
        }

        private void OnDestroy()
        {
            FirstPersonViewer fpv = ServiceLocator.GetService<FirstPersonViewer>();
            fpv.OnViewerEntered -= ViewerEnterd;
            fpv.OnViewerExited -= ViewerExited;
        }

        private void ViewerEnterd() => onViewerEnterd.Invoke();
        private void ViewerExited() => onViewerExited.Invoke();
    }
}
