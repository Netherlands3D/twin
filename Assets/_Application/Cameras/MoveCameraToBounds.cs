using System;
using Netherlands3D.Coordinates;
using Netherlands3D.Twin.FloatingOrigin;
using Netherlands3D.Twin.Utility;
using UnityEngine;

namespace Netherlands3D.Twin.Cameras
{
    public class MoveCameraToBounds : MonoBehaviour
    {
        [SerializeField] private float moveSizeLimit = 20000f; //if the bounds are larger than this number, the camera won't move.
        [SerializeField] private float zoomSizeLimit = 2000f; //if the bounds are larger than this number, the camera won't zoom to fit the bounds on the screen, but will use the default distance.
        [SerializeField] private float defaultCameraDistance = 300f; //default distance of the camera when zooming to the extents is not possible. 

        private Camera camera;
        
        private void Awake()
        {
            camera = GetComponent<Camera>();
        }

        public void MoveToBounds(BoundingBox bounds)
        {
            if (bounds == null) 
            {
                throw new NullReferenceException("Bounds object is null, no bounds specified to center to.");
            }
            
            //move the camera to the center of the bounds, and move it back by the size of the bounds (2x the extents)
            var center = bounds.Center;
            var sizeMagnitude = bounds.GetSizeMagnitude(); //sizeMagnitude returns 2x the extents

            if (sizeMagnitude > moveSizeLimit) // if the size of the bounds is larger than 20km, we don't move the camera
            {
                Debug.LogWarning("Extents too large, not moving camera");
                return;
            }

            // Keep the current camera orientation
            Vector3 cameraDirection = camera.transform.forward;
            double distance = defaultCameraDistance;

            //if the object is smaller than 2km in diameter, we will center the object in the view.
            //if the size of the bounds is larger than 2 km, we will center on the object with a fixed distance instead of trying to fit the object in the view
            if (sizeMagnitude < zoomSizeLimit)
            {
                print("centering on object");
                // Compute the necessary distance to fit the entire object in view
                var fovRadians = camera.fieldOfView * Mathf.Deg2Rad;
                distance = sizeMagnitude / (2 * Mathf.Tan(fovRadians / 2));
            }
            else
            {
                print("using default dist");
            }
            
            var currentCameraPosition = camera.GetComponent<WorldTransform>().Coordinate;
            var difference = (currentCameraPosition - center).Convert(CoordinateSystem.RD); //use RD since this expresses the difference in meters, so we can use the SqrDistanceBeforeShifting to check if we need to shift.
            ulong sqDist = (ulong)(difference.easting * difference.easting + difference.northing * difference.northing);
            if (sqDist > Origin.current.SqrDistanceBeforeShifting) //this distance is not exact since there is still an offset we will apply to the camera, but close enough to fix the issue of floating point errors.
            {
                // move the origin to the bounds center with height 0, to assure large jumps do not result in errors when centering.
                var newOrigin = center.Convert(CoordinateSystem.WGS84); //2d coord system to get rid of height.
                Origin.current.MoveOriginTo(newOrigin);
            }

            camera.transform.position = center.ToUnity() - cameraDirection * (float)distance; //we can now use unity coordinates, as the origin has been shifted if needed.
        }
    }
}
