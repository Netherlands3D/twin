using Netherlands3D.Coordinates;
using Netherlands3D.Twin.Cameras;
using Netherlands3D.Twin.FloatingOrigin;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Netherlands3D
{
    public class CameraWorldTransformShifter : GameObjectWorldTransformShifter
    {
        private FreeCamera camera;
        private Vector3 previousPosition;

        private void Awake()
        {
            camera = GetComponent<FreeCamera>();    
            //WorldTransform transform = camera.gameObject.GetComponent<WorldTransform>();
            //transform.onPostShift.AddListener(OnPostShift);
        }

        public override void PrepareToShift(WorldTransform worldTransform, Coordinate fromOrigin, Coordinate toOrigin)
        {
            //camera.lockUpdateWorldPoint = true;
            //previousPosition = worldTransform.Coordinate.ToUnity();
        }

        public override void ShiftTo(WorldTransform worldTransform, Coordinate fromOrigin, Coordinate toOrigin)
        {
            base.ShiftTo(worldTransform, fromOrigin, toOrigin);

            
        }

        //private void OnPostShift(WorldTransform worldTransform, Coordinate coordinate)
        //{
        //    camera.lockUpdateWorldPoint = false;
        //    //var screenPoint = Pointer.current.position.ReadValue();
        //    //Plane worldPlane = new Plane(Vector3.up, Vector3.zero);
        //    //var screenRay = Camera.main.ScreenPointToRay(screenPoint);
        //    //worldPlane.Raycast(screenRay, out float distance);
        //    //worldTransform.MoveToCoordinate(new Coordinate(screenRay.GetPoint(Mathf.Min(float.MaxValue, distance))));
        //    Vector3 currentPosition = worldTransform.Coordinate.ToUnity();
        //    worldTransform.MoveToCoordinate(new Coordinate(new Vector3(currentPosition.x, previousPosition.y, currentPosition.z)));
        //    camera.UpdateZoomVector();
        //}
    }
}
