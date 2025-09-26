using Netherlands3D.FirstPersonViewer.Events;
using Netherlands3D.Minimap;
using System.Collections;
using UnityEngine;

namespace Netherlands3D.FirstPersonViewer.UI
{
    public class ViewerUIButtons : MonoBehaviour
    {
        [SerializeField] private MinimapUI minimap;
        [SerializeField] private int zoomScale = 7;
        [SerializeField] private Camera2DFrustum frustum;
        [SerializeField] private WMTSMap wmtsMap;

        private void OnEnable()
        {
            StartCoroutine(SetMinimap());
        }
        
        private IEnumerator SetMinimap()
        {
            yield return null;
            minimap.SetZoom(zoomScale);
            frustum.SetActiveCamera(FirstPersonViewerData.Instance.FPVCamera);
            wmtsMap.SetActiveCamera(FirstPersonViewerData.Instance.FPVCamera);
        }


        public void ExitViewer() => ViewerEvents.OnViewerExited?.Invoke();
        public void ResetToStart() => ViewerEvents.OnResetToStart?.Invoke();
    }
}
