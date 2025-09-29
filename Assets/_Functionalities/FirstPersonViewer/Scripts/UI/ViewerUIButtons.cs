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
            StartCoroutine(SetupViewer());
        }

        private void OnDisable()
        {
            snapshotComponent.SetActiveCamera(Camera.main);
        }

        //We need to wait 1 frame to allow the map to load. Prob a temp FIX.
        private IEnumerator SetupViewer()
        {
            yield return null;
            Camera activeCam = FirstPersonViewerData.Instance.FPVCamera;
            
            minimap.SetZoom(zoomScale);

            frustum.SetActiveCamera(activeCam);
            wmtsMap.SetActiveCamera(activeCam);

            snapshotComponent.SetActiveCamera(activeCam);
        }


        public void ExitViewer() => ViewerEvents.OnViewerExited?.Invoke();
        public void ResetToStart() => ViewerEvents.OnResetToStart?.Invoke();
        public void HideUI() => ViewerEvents.OnHideUI?.Invoke();
    }
}