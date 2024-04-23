using Netherlands3D.Coordinates;
using UnityEngine;

namespace Netherlands3D.Twin.FloatingOrigin
{
    public abstract class WorldTransformShifter : MonoBehaviour 
    {
        public abstract void PrepareToShift(WorldTransform worldTransform, Coordinate from, Coordinate to);
        public abstract void ShiftTo(WorldTransform worldTransform, Coordinate from, Coordinate to);
    }
}