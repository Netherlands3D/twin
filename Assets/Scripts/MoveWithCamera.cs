using UnityEngine;

namespace Netherlands3D.Twin
{
    public class MoveWithCamera : MonoBehaviour
    {
        [SerializeField]
        private Camera cameraToFollow;

        private void Start()
        {
            if (cameraToFollow == null)
            {
                cameraToFollow = Camera.main;
            }
        }

        private void Update()
        {
            var cameraPosition = cameraToFollow.transform.position;
            transform.position = new Vector3(cameraPosition.x, transform.position.y, cameraPosition.z);
        }
    }
}