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
        }

        public override void ShiftTo(WorldTransform worldTransform, Coordinate fromOrigin, Coordinate toOrigin)
        {
            base.ShiftTo(worldTransform, fromOrigin, toOrigin);
            Vector3 from = fromOrigin.ToUnity();
            Vector3 to = toOrigin.ToUnity();
            Vector3 offset = to - from;
            camera.UpdateWorldPoint(offset);
        }
    }
}
