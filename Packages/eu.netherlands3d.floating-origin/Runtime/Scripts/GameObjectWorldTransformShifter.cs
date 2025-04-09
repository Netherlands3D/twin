using Netherlands3D.Coordinates;
using UnityEngine;

namespace Netherlands3D.Twin.FloatingOrigin
{
    public class GameObjectWorldTransformShifter : WorldTransformShifter
    {
        public override void PrepareToShift(WorldTransform worldTransform, Coordinate fromOrigin, Coordinate toOrigin)
        {
        }

        public override void ShiftTo(WorldTransform worldTransform, Coordinate fromOrigin, Coordinate toOrigin)
        {
            worldTransform.RecalculatePositionAndRotation();
#if UNITY_EDITOR
            if (worldTransform.Origin.LogShifts) Debug.Log($"<color=grey>{gameObject.name}: Shifting from {transform.position} to {worldTransform.Coordinate.ToUnity()}</color>");
#endif
            transform.hasChanged = false;
        }
    }
}