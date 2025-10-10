using Netherlands3D.FirstPersonViewer;
using Netherlands3D.Minimap;
using System.Collections;
using UnityEngine;

using SnapshotComponent = Netherlands3D.Snapshots.Snapshots;

namespace Netherlands3D
{
    public class FirstPersonViewerSetup : MonoBehaviour
    {
        [Header("Minimap")]
        [SerializeField] private MinimapUI minimap;
        [SerializeField] private int zoomScale = 7;
        [Space()]
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
            frustum.SetActiveCamera(Camera.main);
            wmtsMap.SetActiveCamera(Camera.main);
            snapshotComponent.SetActiveCamera(Camera.main);
        }

        //We need to wait 1 frame to allow the map to load or we get an unloaded map that's zoomed in.
        private IEnumerator SetupViewer()
        {
            yield return null;
            Camera activeCam = FirstPersonViewerData.Instance.FPVCamera;

            minimap.SetZoom(zoomScale);

            frustum.SetActiveCamera(activeCam);
            wmtsMap.SetActiveCamera(activeCam);

            snapshotComponent.SetActiveCamera(activeCam);
        }
    }
}
