using UnityEngine.InputSystem;
using UnityEngine;
using System;
using Netherlands3D.Coordinates;
using Netherlands3D.Services;

namespace Netherlands3D.Twin.Samplers
{
    public class PointerToWorldPosition : MonoBehaviour
    {       
        public Coordinate WorldPoint => worldPoint;
        public bool debugHeightmapPosition = false;
        
        private OpticalRaycaster opticalRaycaster;
        private Action<Vector3, bool> worldPointCallback;
        private Coordinate worldPoint;
        private float maxDistance = 10000;

        private GameObject testPosition;
        
        private void Awake()
        {
            opticalRaycaster = GetComponent<OpticalRaycaster>();
        }

        private void Start()
        {
            worldPointCallback = (w,h) =>
            {
                if (h)
                    worldPoint = new Coordinate(w);
                else
                {
                    Vector3 position = GetWorldPoint();
                    worldPoint = new Coordinate(position);
                }
            };
        }

        private void Update()
        {
            var screenPoint = Pointer.current.position.ReadValue();
            opticalRaycaster.GetWorldPointAsync(screenPoint, worldPointCallback);

            if(debugHeightmapPosition)
            {
                if(testPosition == null)
                {
                    testPosition = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    testPosition.transform.localScale = Vector3.one * 10;
                    testPosition.GetComponent<Renderer>().material.color = Color.green;
                }
                testPosition.transform.position = GetWorldPoint();
            }
            else if(testPosition != null)
            {
                Destroy(testPosition);
            }
        }

        public void GetPointerWorldPointAsync(Action<Vector3> result)
        {
            var screenPoint = Pointer.current.position.ReadValue();
            opticalRaycaster.GetWorldPointAsync(screenPoint, (point, hit) =>
            {
                if (hit)
                    result.Invoke(point);
                else
                {
                    Vector3 position = GetWorldPoint();
                    result.Invoke(position);
                }  
            });
        }

        public Vector3 GetWorldPoint()
        {
            var screenPoint = Pointer.current.position.ReadValue();
            return GetWorldPoint(screenPoint);
        }

        public Vector3 GetWorldPointCenterView()
        {
            var screenPoint = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            return GetWorldPoint(screenPoint);
        }

        public Vector3 GetWorldPoint(Vector2 screenPosition)
        {            
            Plane worldPlane = new Plane(Vector3.up, Vector3.zero);
            var screenRay = Camera.main.ScreenPointToRay(screenPosition);
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
            Vector3 origin = Camera.main.transform.position;
            Vector3 dir = screenRay.direction;
            float t = (height - origin.y) / dir.y;
            Vector3 intersection = origin + dir * t;
            return intersection;
        }
    }
}
