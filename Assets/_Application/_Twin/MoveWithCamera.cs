using UnityEngine;

namespace Netherlands3D.Twin
{
    public class MoveWithCamera : MonoBehaviour
    {
        [SerializeField] private Camera cameraToFollow;
        private Transform cameraTransform;
        [SerializeField] private bool centerOfViewport = false;

        private void Start()
        {
            if (cameraToFollow == null)
            {
                cameraToFollow = Camera.main;
            }
            cameraTransform = cameraToFollow.transform;
        }

        private void Update()
        {
            var cameraPosition = cameraTransform.position;

            if (centerOfViewport)
            {
                var ray = cameraToFollow.ScreenPointToRay(new Vector2(Screen.width * .5f, Screen.height * .5f));
                var plane = new Plane(Vector3.up, 0);
                if (plane.Raycast(ray, out var distance) == false)
                {
                    distance = 10f;
                }

                cameraPosition += cameraTransform.forward * distance;
            }

            transform.position = new Vector3(cameraPosition.x, transform.position.y, cameraPosition.z);
        }
    }
}