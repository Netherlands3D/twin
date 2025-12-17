using Netherlands3D.Services;
using UnityEngine;

namespace Netherlands3D.FirstPersonViewer.Miscellaneous
{
    //TODO: Should be handled with one LookAtCamera script, but that script uses the Camera.Main and there is no CameraManager yet.
    public class FPVBillboard : MonoBehaviour
    {
        private Camera fpvCamera;

        private void Start()
        {
            fpvCamera = FirstPersonViewerCamera.FPVCamera;
        }

        private void Update()
        {
            Vector3 dir = transform.position - fpvCamera.transform.position;
            transform.rotation = Quaternion.LookRotation(dir);
        }
    }
}
