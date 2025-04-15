using System;
using Netherlands3D.Coordinates;
using Netherlands3D.Twin.FloatingOrigin;
using Netherlands3D.Twin.Layers.Properties;
using UnityEngine;

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
        
        private void Awake()
        {
            camera = GetComponent<Camera>();
            cameraWorldTransform = GetComponent<WorldTransform>();
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
            
            //to avoid floating point issues, we need to calculate the relative offset and add it to the targetLookAt
            // todo: to make this more compatible with different coordinate systems, we need to know the axis indices and add it to the correct one, but these are currently not publicly exposed.
            var offset = cameraDirection * (float)distance;
            var targetCoordinate = new Coordinate(targetLookAt.CoordinateSystem,  
                                                targetLookAt.value1 - (double)offset.x,
                                                targetLookAt.value2 - (double)offset.z,
                                                targetLookAt.value3 - (double)offset.y);

            // Set the coordinate of the worldTransform, and thereby the Camera's unity position.
            // The Camera's Unity position will cause floating point issues, however it is only needed to trigger the Origin shift,
            // and then the correct position will be recalculated from the world transform's coordinate.
            cameraWorldTransform.MoveToCoordinate(targetCoordinate); //update the coordinate 
        }

        public void LoadCameraData(CameraPropertyData cameraPropertyData)
        {    
            camera.orthographic = cameraPropertyData.Orthographic;
            cameraWorldTransform.MoveToCoordinate(cameraPropertyData.Position);
            cameraWorldTransform.SetRotation(Quaternion.Euler(cameraPropertyData.EulerRotation));
        }
    }
}