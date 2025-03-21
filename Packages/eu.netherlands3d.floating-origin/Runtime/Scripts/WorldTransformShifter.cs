using Netherlands3D.Coordinates;
using UnityEngine;

namespace Netherlands3D.Twin.FloatingOrigin
{
    public abstract class WorldTransformShifter : MonoBehaviour 
    {
        public abstract void PrepareToShift(WorldTransform worldTransform, Coordinate fromOrigin, Coordinate toOrigin);
        public abstract void ShiftTo(WorldTransform worldTransform, Coordinate fromOrigin, Coordinate toOrigin);
    }
}