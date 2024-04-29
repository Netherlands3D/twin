using Netherlands3D.Coordinates;
using UnityEngine;

namespace Netherlands3D.Twin.FloatingOrigin
{
    public class GameObjectWorldTransformShifter : WorldTransformShifter
    {
        public override void PrepareToShift(WorldTransform worldTransform, Coordinate from, Coordinate to)
        {
            // Doesn't need to do anything prior to shifting
        }

        public override void ShiftTo(WorldTransform worldTransform, Coordinate from, Coordinate to)
        {
            // We can just recalculate the transform position based on the real world Coordinate.
            var newPosition = CoordinateConverter
                .ConvertTo(worldTransform.Coordinate, CoordinateSystem.Unity)
                .ToVector3();

#if UNITY_EDITOR
            if (worldTransform.Origin.LogShifts) Debug.Log($"<color=grey>{gameObject.name}: Shifting from {transform.position} to {newPosition}</color>");
#endif

            transform.position = newPosition;
        }
    }
}