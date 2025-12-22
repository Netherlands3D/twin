using UnityEngine;

namespace Netherlands3D
{
    public class HideTouchScreen : MonoBehaviour
    {
        private void Start()
        {
            gameObject.SetActive(WebGLOsDetection.IsMobile());
        }
    }
}
