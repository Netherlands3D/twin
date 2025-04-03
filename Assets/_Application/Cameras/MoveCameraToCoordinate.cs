using System;
using Netherlands3D.Coordinates;
using Netherlands3D.Twin.FloatingOrigin;
using Netherlands3D.Twin.Utility;
using UnityEngine;

namespace Netherlands3D.Twin.Cameras
{
    [RequireComponent(typeof(Camera))]
    [RequireComponent(typeof(FreeCamera))]
    [RequireComponent(typeof(WorldTransform))]
    public class MoveCameraToCoordinate : MonoBehaviour
    {
        [Tooltip("if the bounds are larger than this number, the camera won't move.")] 
        [SerializeField] private float moveSizeLimit = 20000f;

        [Tooltip("if the bounds are larger than this number, the camera won't zoom to fit the bounds on the screen, but will use the default distance.")] 
        [SerializeField] private float zoomSizeLimit = 2000f;

        [Tooltip("default distance of the camera when zooming to the extents is not possible. ")] 
        [SerializeField] private float defaultCameraDistance = 300f;

        [Tooltip("multiply the zoom distance for objects that are zoomed to by this amount so the objects don't take up the entire screen")] 
        [SerializeField] private float zoomDistanceMultiplier = 4f;
        [SerializeField] double decayRate = 0.007d; // Decay rate

        private new Camera camera;
        private FreeCamera cameraMover; //move through the FreeCamera script to make sure we only move the camera through a single point of entry
        private WorldTransform cameraWorldTransform;
        
        private void Awake()
        {
            camera = GetComponent<Camera>();
            cameraMover = GetComponent<FreeCamera>();
            cameraWorldTransform = GetComponent<WorldTransform>();
        }

        public void MoveToCoordinate(Coordinate coordinate)
        {
            ShiftOriginIfNeeded(coordinate);
            cameraMover.MoveToTarget(coordinate.ToUnity()); //we can now use unity coordinates, as the origin has been shifted if needed.
        }

        public void LookAtTarget(Coordinate targetLookAt, double targetDistance)
        {
            if (targetDistance > moveSizeLimit) // if the size of the bounds is larger than 20km, we don't move the camera
            {
                Debug.LogWarning("target distance too large, not moving camera");
                return;
            }

            // Keep the current camera orientation
            Vector3 cameraDirection = camera.transform.forward;
            double distance = defaultCameraDistance;

            //if the object is smaller than 2km in diameter, we will center the object in the view.
            //if the size of the bounds is larger than 2 km, we will center on the object with a fixed distance instead of trying to fit the object in the view
            if (targetDistance < zoomSizeLimit)
            {
                // Compute the necessary distance to fit the entire object in view
                var fovRadians = camera.fieldOfView * Mathf.Deg2Rad;
                distance = targetDistance / (2 * Mathf.Tan(fovRadians * 0.5f));
                var distanceFactor = Math.Exp(-decayRate * distance); //if an object is larger, we move away less.
                var t = Mathf.Lerp(1, zoomDistanceMultiplier, (float)distanceFactor);
                distance *= t; //increase distance so that objects don't take up too much screen space
            }

            ShiftOriginIfNeeded(targetLookAt); //this distance is not exact since there is still an offset we will apply to the camera, but close enough to fix the issue of floating point errors.

            var unityTargetPosition = targetLookAt.ToUnity() - cameraDirection * (float)distance;
            cameraMover.MoveToTarget(unityTargetPosition); //we can now use unity coordinates, as the origin has been shifted if needed.
        }

        private void ShiftOriginIfNeeded(Coordinate targetCoordinate)
        {
            Vector3 currentCameraPosition = cameraWorldTransform.Coordinate.ToUnity();
            Vector3 target = targetCoordinate.ToUnity();
            Vector3 difference = currentCameraPosition - target;
            ulong sqDist = (ulong)(difference.x * difference.x + difference.z * difference.z);
            if (sqDist > Origin.current.SqrDistanceBeforeShifting)
            {
                // move the origin to the bounds center with height 0, to assure large jumps do not result in errors when centering.
                //var origin = new Coordinate(new Vector3(target.x, 0, target.z));
                var newOrigin = targetCoordinate.Convert(CoordinateSystem.WGS84_LatLon);
                Origin.current.MoveOriginTo(newOrigin);
            }
        }
    }
}