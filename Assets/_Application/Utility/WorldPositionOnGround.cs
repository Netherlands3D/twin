using Netherlands3D.Services;
using Netherlands3D.Twin.Samplers;
using System.Collections;
using Netherlands3D.Twin.Cameras;
using UnityEngine;

namespace Netherlands3D.Twin
{
    public class WorldPositionOnGround : MonoBehaviour
    {
        private OpticalRaycaster opticalRaycaster;
        private bool positionFound = false;
        private Vector3 worldXZ;
        private WaitForSeconds waitSecond = new WaitForSeconds(1f);
        private CameraService cameraService;

        void Awake()
        {
            cameraService = App.Cameras;
        }
        
        void Start()
        {
            opticalRaycaster = ServiceLocator.GetService<OpticalRaycaster>();
            worldXZ = new Vector3(transform.position.x, 0, transform.position.z);
            StartCoroutine(UpdatePositionCheck());
        }

        private IEnumerator UpdatePositionCheck()
        {
            while(!positionFound && this != null)
            {
                Vector3 screenPoint = cameraService.ActiveCamera.WorldToScreenPoint(worldXZ);
                var ray = cameraService.ActiveCamera.ScreenPointToRay(screenPoint);
                var isHit = opticalRaycaster.Raycast(ray.origin, ray.direction, out var worldPoint, 1 << LayerMask.NameToLayer("Terrain"));
                Callback(worldPoint, isHit);
                yield return waitSecond;
            }
            if(positionFound)
                transform.position = worldXZ;
        }

        private void Callback(Vector3 hitWorldPoint, bool hasHit)
        {
            if (hasHit)
            {
                positionFound = true;
                worldXZ.y = hitWorldPoint.y;
            }
        }
        
        public void SetDirty()
        {
            positionFound = false;
        }
    }
}
