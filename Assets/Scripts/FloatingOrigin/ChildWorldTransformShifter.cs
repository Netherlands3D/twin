using Netherlands3D.Coordinates;
using UnityEngine;

namespace Netherlands3D.Twin.FloatingOrigin
{
    public class ChildWorldTransformShifter : WorldTransformShifter
    {
        public override void ShiftTo(WorldTransform worldTransform, Coordinate from, Coordinate to)
        {
            // Calculate the shift in Unity Coordinates and move all child tiles in Unity coordinates
            var delta = CoordinateConverter.ConvertTo(from, CoordinateSystem.Unity).ToVector3() 
                - CoordinateConverter.ConvertTo(to, CoordinateSystem.Unity).ToVector3();
            
            foreach (Transform child in transform)
            {
                child.position += delta;
            }
        }
    }
}