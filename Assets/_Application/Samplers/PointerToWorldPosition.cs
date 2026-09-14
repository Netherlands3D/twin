using UnityEngine.InputSystem;
using UnityEngine;
using Netherlands3D.Coordinates;
using Netherlands3D.Services;
using Netherlands3D.Twin.Cameras;

namespace Netherlands3D.Twin.Samplers
{
    public class PointerToWorldPosition : MonoBehaviour
    {      
        public bool debugHeightmapPosition = false;

        private OpticalRaycaster opticalRaycaster;
        private Vector3 worldPointHeightMap;
        private float maxDistance = 10000;

        private GameObject testPosition;

        private CachedOpticalWorldPoint cachedOpticalWorldPoint;
        private CameraService cameraService;

        private struct CachedOpticalWorldPoint
        {
            public int FrameCount;
            public Vector2 PointerPosition;
            public Vector3 PointerWorldPosition;
        }

        private void Awake()
        {
            opticalRaycaster = GetComponent<OpticalRaycaster>();
            cameraService = App.Cameras;
        }

        void Update()
        {
            if (debugHeightmapPosition)
            {
                var screenPoint = Pointer.current.position.ReadValue();
                if (testPosition == null)
                {
                    testPosition = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    testPosition.transform.localScale = Vector3.one * 10;
                    testPosition.GetComponent<Renderer>().material.color = Color.green;
                }

                testPosition.transform.position = GetWorldPointUsingHeightMap(screenPoint);;
            }
            else if(testPosition != null)
            {
                Destroy(testPosition);
            }
        }
        
        public Vector3 GetWorldPointUsingOpticalRaycaster()
        {
            return GetOrCalculateOpticalWorldPoint();
        }
        
        /// <summary>
        /// Gets worldPoint underneath the pointer using the heightMap texture.
        /// </summary>
        public Vector3 GetWorldPointUsingHeightMap()
        {
            var screenPoint = Pointer.current.position.ReadValue();
            return GetWorldPointUsingHeightMap(screenPoint);
        }

        public Vector3 GetWorldPointCenterViewUsingHeightMap()
        {
            var screenPoint = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            return GetWorldPointUsingHeightMap(screenPoint);
        }

        /// <summary>
        /// Gets worldPoint using the heightMap texture.
        /// </summary>
        public Vector3 GetWorldPointUsingHeightMap(Vector2 screenPosition)
        {
            var activeCamera = cameraService.ActiveCamera;
            
            Plane worldPlane = new Plane(Vector3.up, Vector3.zero);
            var screenRay = activeCamera.ScreenPointToRay(screenPosition);
            worldPlane.Raycast(screenRay, out float distance);
            Vector3 position;
            //when no valid point is found in for the raycast, lets invert the distance so we get a point in the sky
            if (distance < 0)
            {
                float length = Mathf.Min(maxDistance, -distance);
                position = screenRay.GetPoint(length);
                return position;
            }
            else
            {
                float length = Mathf.Min(maxDistance, distance);
                position = screenRay.GetPoint(length);
            }

            Coordinate initialCoordinate = new Coordinate(position);
            HeightMap heightMap = ServiceLocator.GetService<HeightMap>();   
            float height = heightMap.GetHeight(initialCoordinate);
            Vector3 origin = activeCamera.transform.position;
            Vector3 dir = screenRay.direction;
            float t = (height - origin.y) / dir.y;
            Vector3 intersection = origin + dir * t;
            return intersection;
        }
        
        private Vector3 GetOrCalculateOpticalWorldPoint()
        {
            if (Time.frameCount == cachedOpticalWorldPoint.FrameCount 
                && Pointer.current.position.ReadValue() == cachedOpticalWorldPoint.PointerPosition)
            {
                return cachedOpticalWorldPoint.PointerWorldPosition;
            }

            cachedOpticalWorldPoint = new CachedOpticalWorldPoint()
            {
                FrameCount = Time.frameCount,
                PointerPosition = Pointer.current.position.ReadValue(),
                PointerWorldPosition = CalculateOpticalWorldPoint()
            };      
            return cachedOpticalWorldPoint.PointerWorldPosition;
        }

        private Vector3 CalculateOpticalWorldPoint()
        {
            var activeCamera = cameraService.ActiveCamera;
            
            var screenPoint = Pointer.current.position.ReadValue();
            Vector3 worldPosition = default;
            var ray = activeCamera.ScreenPointToRay(screenPoint);
            if (opticalRaycaster.Raycast(ray.origin, ray.direction, out var hitPoint))
            {
                worldPosition = hitPoint;
            }
            else
            {
                var worldPositionHeightMap = GetWorldPointUsingHeightMap(screenPoint);
                worldPosition = worldPositionHeightMap;
            }
           
            return worldPosition;
        }
    }
}
