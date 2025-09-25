using Netherlands3D.FirstPersonViewer;
using Netherlands3D.Twin.Samplers;
using UnityEngine;

namespace Netherlands3D
{
    public class ViewCameraSwitcher : MonoBehaviour
    {
        private PointerToWorldPosition pointerToWorld;

        private void OnEnable()
        {
            if(pointerToWorld == null) pointerToWorld = FindFirstObjectByType<PointerToWorldPosition>();
            pointerToWorld.SetActiveCamera(FirstPersonViewerData.Instance.FPVCamera);
        }

        private void OnDisable()
        {
            pointerToWorld.SetActiveCamera(Camera.main);
        }
    }
}
