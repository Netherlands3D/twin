using System;
using Netherlands3D.Coordinates;
using Netherlands3D.Twin.FloatingOrigin;
using Netherlands3D.Twin.Layers.Properties;
using UnityEngine;
using UnityEngine.Events;

namespace Netherlands3D.Twin.Cameras
{
    [RequireComponent(typeof(Camera))]
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
        private WorldTransform cameraWorldTransform;

        public UnityEvent<bool> orthographicLoaded;
        
        private void Awake()
        {
            camera = GetComponent<Camera>();
            cameraWorldTransform = GetComponent<WorldTransform>();
        }

        public void LookAtTarget(Coordinate targetLookAt, double targetDistance, bool clampToLimits = false)
        {
            // Keep the current camera orientation
            Vector3 cameraDirection = camera.transform.forward;
            double distance = defaultCameraDistance;

            if (clampToLimits)
            {
                // New behavior: clamp the size to the limits instead of skipping or using the default distance.
                double clampedTargetDistance = Math.Min(targetDistance, moveSizeLimit);
                double clampedDistance = Math.Min(clampedTargetDistance, zoomSizeLimit);
                distance = CalculateFitDistance(clampedDistance);
            }
            else
            {
                // Old behavior: don't move at all if the target is larger than the move limit.
                if (targetDistance > moveSizeLimit)
                {
                    Debug.LogWarning("target distance too large, not moving camera");
                    return;
                }

                // Only fit the object in view if it is smaller than zoomSizeLimit,
                // otherwise keep the default camera distance.
                if (targetDistance < zoomSizeLimit)
                {
                    distance = CalculateFitDistance(targetDistance);
                }
            }

            // To avoid floating point issues, calculate the relative offset and add it to targetLookAt.
            // todo: to make this more compatible with different coordinate systems, we need to know the axis indices and add it to the correct one, but these are currently not publicly exposed.
            var offset = cameraDirection * (float)distance;
            var targetCoordinate = new Coordinate(targetLookAt.CoordinateSystem,
                                                targetLookAt.value1 - (double)offset.x,
                                                targetLookAt.value2 - (double)offset.z,
                                                targetLookAt.value3 - (double)offset.y);

            // Set the coordinate of the worldTransform, and thereby the Camera's Unity position.
            // The Camera's Unity position will cause floating point issues, however it is only needed to trigger the Origin shift,
            // and then the correct position will be recalculated from the world transform's coordinate.
            cameraWorldTransform.MoveToCoordinate(targetCoordinate);
        }

        private double CalculateFitDistance(double size)
        {
            // Compute the necessary distance to fit the entire object in view
            double fovRadians = camera.fieldOfView * Mathf.Deg2Rad;
            double distance = size / (2 * Math.Tan(fovRadians * 0.5));

            // If an object is larger, we move away less.
            double distanceFactor = Math.Exp(-decayRate * distance);
            float t = Mathf.Lerp(1, zoomDistanceMultiplier, (float)distanceFactor);

            // Increase distance so that objects don't take up too much screen space.
            return distance * t;
        }
        

        public void LoadCameraData(CameraPropertyData cameraPropertyData)
        {    
            camera.orthographic = cameraPropertyData.Orthographic;
            orthographicLoaded.Invoke(cameraPropertyData.Orthographic);
            
            cameraWorldTransform.MoveToCoordinate(cameraPropertyData.Position);
            cameraWorldTransform.SetRotation(Quaternion.Euler(cameraPropertyData.EulerRotation));
        }
    }
}