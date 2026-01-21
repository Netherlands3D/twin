using Netherlands3D.Minimap;
using Netherlands3D.Services;
using Netherlands3D.Twin.UI;
using System.Collections;
using UnityEngine;

using SnapshotComponent = Netherlands3D.Snapshots.Snapshots;

namespace Netherlands3D.FirstPersonViewer.UI
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
            TransformHandleInterfaceToggle handle = ServiceLocator.GetService<TransformHandleInterfaceToggle>();
            if (handle != null) handle.SetTransformHandleEnabled(false);

            StartCoroutine(SetupViewer());
        }

        private void OnDisable()
        {
            frustum.SetActiveCamera(Camera.main);
            wmtsMap.SetActiveCamera(Camera.main);
            
            //When stopping Unity without null check this will always throw an error.
            ServiceLocator.GetService<SnapshotComponent>()?.SetActiveCamera(Camera.main);
            ServiceLocator.GetService<TransformHandleInterfaceToggle>()?.SetTransformHandleEnabled(true);
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
