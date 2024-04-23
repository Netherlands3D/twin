using Netherlands3D.Coordinates;
using UnityEngine;

namespace Netherlands3D.Twin.FloatingOrigin
{
    public class CameraWorldTransformShifter : WorldTransformShifter
    {
        public override void PrepareToShift(WorldTransform worldTransform, Coordinate from, Coordinate to)
        {
            // Doesn't need to do anything prior to shifting
        }

        public override void ShiftTo(WorldTransform worldTransform, Coordinate from, Coordinate to)
        {
            // Cameras stay at the same Unity location and change their Coordinates when the Origin move, regular
            // objects' Coordinate does not change when the Origin moves because the Coordinate represents their real
            // world location

            var delta = CoordinateConverter.ConvertTo(from, CoordinateSystem.Unity).ToVector3() -
                CoordinateConverter.ConvertTo(to, CoordinateSystem.Unity).ToVector3();

            var unityTransform = worldTransform.transform;
            var newPosition = unityTransform.position;
            newPosition += delta;
            
#if UNITY_EDITOR
            if (worldTransform.Origin.LogShifts) Debug.Log($"<color=grey>{gameObject.name}: Shifting from {transform.position} to {newPosition}</color>");
#endif
            
            unityTransform.position = newPosition;

            worldTransform.Coordinate = CoordinateConverter.ConvertTo(
                new Coordinate(CoordinateSystem.Unity, newPosition.x, newPosition.y, newPosition.z),
                worldTransform.ReferenceCoordinateSystem
            );
        }
    }
}