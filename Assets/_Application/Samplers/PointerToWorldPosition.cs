using UnityEngine.InputSystem;
using UnityEngine;
using Netherlands3D.Coordinates;
using Netherlands3D.Services;

namespace Netherlands3D.Twin.Samplers
{
    public class PointerToWorldPosition : MonoBehaviour
    {      
        public bool debugHeightmapPosition = false;
        
        private OpticalRaycaster opticalRaycaster;
        private Vector3 worldPointHeightMap;
        private float maxDistance = 10000;

        private GameObject testPosition;
        private Camera activeCamera;

        private CachedOpticalWorldPoint cachedOpticalWorldPoint;

        private struct CachedOpticalWorldPoint
        {
            public int FrameCount;
            public Vector2 PointerPosition;
            public Vector3 PointerWorldPosition;
        }

        private void Awake()
        {
            opticalRaycaster = GetComponent<OpticalRaycaster>();
        }

        private void Start()
        {
            activeCamera = App.Cameras.ActiveCamera;
            App.Cameras.OnSwitchCamera.AddListener(SetActiveCamera);
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

                testPosition.transform.position = GetWorldPoint(screenPoint, activeCamera);;
            }
            else if(testPosition != null)
            {
                Destroy(testPosition);
            }
        }
        
        public Vector3 GetOpticalWorldPoint()
        {
            return GetOrCalculateOpticalWorldPoint();
        }
        
        /// <summary>
        /// Gets worldPoint underneath the pointer using the heightMap texture.
        /// </summary>
        public Vector3 GetWorldPoint()
        {
            var screenPoint = Pointer.current.position.ReadValue();
            return GetWorldPoint(screenPoint, activeCamera);
        }
        
        /// <summary>
        /// Gets worldPoint using the heightMap texture.
        /// </summary>
        public Vector3 GetWorldPoint(Vector2 screenPosition)
        {
           return GetWorldPoint(screenPosition, activeCamera);
        }

        public Vector3 GetWorldPointCenterView()
        {
            var screenPoint = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            return GetWorldPoint(screenPoint, activeCamera);
        }

        /// <summary>
        /// Gets worldPoint using the heightMap texture.
        /// </summary>
        public Vector3 GetWorldPoint(Vector2 screenPosition, Camera camera)
        {            
            Plane worldPlane = new Plane(Vector3.up, Vector3.zero);
            var screenRay = camera.ScreenPointToRay(screenPosition);
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

        public void SetActiveCamera(Camera camera) => activeCamera = camera;
        
        private Vector3 GetOrCalculateOpticalWorldPoint()
        {
            if (Time.frameCount == cachedOpticalWorldPoint.FrameCount || Pointer.current.position.ReadValue() == cachedOpticalWorldPoint.PointerPosition)
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
            var screenPoint = Pointer.current.position.ReadValue();
            Vector3 worldPosition = default;
            if (opticalRaycaster.TryGetWorldPoint(activeCamera, screenPoint, out var hitPoint))
            {
                worldPosition = hitPoint;
            }
            else
            {
                var worldPositionHeightMap = GetWorldPoint(screenPoint, activeCamera);
                worldPosition = worldPositionHeightMap;
            }
           
            return worldPosition;
        }
        
        private void OnDestroy()
        {
            App.Cameras.OnSwitchCamera.RemoveListener(SetActiveCamera);
        }
    }
}
