using Netherlands3D.FirstPersonViewer;
using Netherlands3D.Minimap;
using Netherlands3D.Services;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

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

        private void OnEnable()
        {
            StartCoroutine(SetupViewer());
        }

        private void OnDisable()
        {
            frustum.SetActiveCamera(Camera.main);
            wmtsMap.SetActiveCamera(Camera.main);
            ServiceLocator.GetService<SnapshotComponent>().SetActiveCamera(Camera.main);
        }

        private IEnumerator SetupViewer()
        {
            //We need to wait 1 frame to allow the map to load or we get an unloaded map that's zoomed in. (That will never load)
            yield return null;
            Camera activeCam = FirstPersonViewerCamera.FPVCamera;

            minimap.SetZoom(zoomScale);

            frustum.SetActiveCamera(activeCam);
            wmtsMap.SetActiveCamera(activeCam);

            ServiceLocator.GetService<SnapshotComponent>().SetActiveCamera(activeCam);
        }

    }
}
