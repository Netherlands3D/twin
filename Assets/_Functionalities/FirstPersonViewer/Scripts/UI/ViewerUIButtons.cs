using Netherlands3D.FirstPersonViewer.Events;
using Netherlands3D.Snapshots;
using Netherlands3D.Minimap;
using System.Collections;
using UnityEngine;

using SnapshotComponent = Netherlands3D.Snapshots.Snapshots;

namespace Netherlands3D.FirstPersonViewer.UI
{
    public class ViewerUIButtons : MonoBehaviour
    {
        [SerializeField] private MinimapUI minimap;
        [SerializeField] private int zoomScale = 7;
        [SerializeField] private Camera2DFrustum frustum;
        [SerializeField] private WMTSMap wmtsMap;

        private SnapshotComponent snapshotComponent;

        private void Awake()
        {
            snapshotComponent = FindAnyObjectByType<SnapshotComponent>();
        }

        private void OnEnable()
        {
            Camera activeCam = FirstPersonViewerData.Instance.FPVCamera;

            frustum.SetActiveCamera(activeCam);
            wmtsMap.SetActiveCamera(activeCam);

            snapshotComponent.SetActiveCamera(activeCam);

            StartCoroutine(SetZoom());
        }

        private void OnDisable()
        {
            snapshotComponent.SetActiveCamera(Camera.main);
        }

        //We need to wait 1 frame to allow the map to load.
        private IEnumerator SetZoom()
        {
            yield return null;
            minimap.SetZoom(zoomScale);
        }


        public void ExitViewer() => ViewerEvents.OnViewerExited?.Invoke();
        public void ResetToStart() => ViewerEvents.OnResetToStart?.Invoke();
        public void HideUI() => ViewerEvents.OnHideUI?.Invoke();
    }
}