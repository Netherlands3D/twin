using Netherlands3D.Services;
using Netherlands3D.Twin;
using UnityEngine;

namespace Netherlands3D.FirstPersonViewer.Miscellaneous
{
    //TODO: Should be handled with one LookAtCamera script, but that script uses the Camera.Main and there is no CameraManager yet.
    public class FPVBillboard : MonoBehaviour
    {
        private void Update()
        {
            Vector3 dir = transform.position - App.Cameras.ActiveCamera.transform.position;
            transform.rotation = Quaternion.LookRotation(dir);
        }
    }
}
