using UnityEngine;

namespace Netherlands3D
{
    public class GeometrySkybox : MonoBehaviour
    {
        private Camera mainCam;

        private void Start()
        {
            mainCam = Camera.main;
        }

        void Update()
        {
            transform.position = mainCam.transform.position;
        }
    }
}