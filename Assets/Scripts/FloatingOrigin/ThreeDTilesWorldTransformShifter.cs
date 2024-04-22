using Netherlands3D.Coordinates;
using Netherlands3D.Tiles3D;
using UnityEngine;

namespace Netherlands3D.Twin.FloatingOrigin
{
    public class ThreeDTilesWorldTransformShifter : WorldTransformShifter
    {
        public override void ShiftTo(WorldTransform worldTransform, Coordinate from, Coordinate to)
        {
            // TODO: Should we prepare shifting so that we can track positions or do stuff before the shift?
            // OnBeforeShift? oid
            
            // Calculate the shift in Unity Coordinates and move all child tiles in Unity coordinates
            var delta = CoordinateConverter.ConvertTo(from, CoordinateSystem.Unity).ToVector3() 
                - CoordinateConverter.ConvertTo(to, CoordinateSystem.Unity).ToVector3();

            // TODO: this does not take the curve into consideration

            var contentComponents = transform.GetComponentsInChildren<Content>();
            foreach (Content contentComponent in contentComponents)
            {
                foreach (Transform child in contentComponent.transform)
                {
                    child.position += delta;
                }
            }
        }
    }
}