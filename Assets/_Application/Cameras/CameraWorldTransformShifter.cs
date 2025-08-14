using Netherlands3D.Coordinates;
using Netherlands3D.Twin.Cameras;
using Netherlands3D.Twin.FloatingOrigin;
using UnityEngine;

namespace Netherlands3D
{
    public class CameraWorldTransformShifter : GameObjectWorldTransformShifter
    {
        private FreeCamera camera;

        private void Awake()
        {
            camera = GetComponent<FreeCamera>();    
        }

        public override void PrepareToShift(WorldTransform worldTransform, Coordinate fromOrigin, Coordinate toOrigin)
        {
            camera.lockUpdateWorldPoint = true;
        }

        public override void ShiftTo(WorldTransform worldTransform, Coordinate fromOrigin, Coordinate toOrigin)
        {
            base.ShiftTo(worldTransform, fromOrigin, toOrigin);
            (double x, double y, double z) from = fromOrigin.ToUnityDouble3();
            (double x, double y, double z) to = toOrigin.ToUnityDouble3();
            (double x, double y, double z) offset = (to.x - from.x, to.y - from.y, to.z - from.z);
            camera.UpdateWorldPoint(new Vector3((float)offset.x, (float)offset.y, (float)offset.z));
            camera.lockUpdateWorldPoint = false;
        }
    }
}
