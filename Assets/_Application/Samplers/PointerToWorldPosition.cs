using UnityEngine.InputSystem;
using UnityEngine;
using System;
using Netherlands3D.Coordinates;

namespace Netherlands3D.Twin.Samplers
{
    public class PointerToWorldPosition : MonoBehaviour
    {       
        public Coordinate WorldPoint => worldPoint;

        private HeightMap heightMap;
        private OpticalRaycaster opticalRaycaster;
        private Action<Vector3, bool> worldPointCallback;
        private Coordinate worldPoint;
        private float maxDistance = 10000;

        private GameObject testPosition;
        public bool debugHeightmapPosition = true;

        private void Awake()
        {
            heightMap = GetComponent<HeightMap>();  
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

        public Vector3 GetWorldPoint(Vector2 screenPosition)
        {            
            Plane worldPlane = new Plane(Vector3.up, Vector3.zero);
            var screenRay = Camera.main.ScreenPointToRay(screenPosition);
            worldPlane.Raycast(screenRay, out float distance);
            Vector3 position;
            //when no valid point is found in for the raycast, lets invert the distance so we get a point in the sky
            if (distance < 0)
                position = screenRay.GetPoint(Mathf.Min(maxDistance, -distance));
            else
                position = screenRay.GetPoint(Mathf.Min(maxDistance, distance));

            Coordinate testCoord = new Coordinate(position);
            float height = heightMap.GetHeight(testCoord);
            Vector3 origin = Camera.main.transform.position;
            Vector3 dir = screenRay.direction;
            float t = (height - origin.y) / dir.y;
            Vector3 intersection = origin + dir * t;
            return intersection;
        }
    }
}
