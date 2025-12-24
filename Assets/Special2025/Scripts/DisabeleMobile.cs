using UnityEngine;

namespace Netherlands3D
{
    public class DisabeleMobile : MonoBehaviour
    {
        private void Start()
        {
            gameObject.SetActive(!WebGLOsDetection.IsMobile());
        }
    }
}
