using UnityEngine;

namespace Netherlands3D
{
    public class HideTouchScreen : MonoBehaviour
    {
        [SerializeField] private GameObject mobileScreen;
        [SerializeField] private GameObject computerScreen;

        private void Start()
        {
            bool isMobile = WebGLOsDetection.IsMobile();

            mobileScreen.SetActive(isMobile);
            computerScreen.SetActive(!isMobile);
        }
    }
}
