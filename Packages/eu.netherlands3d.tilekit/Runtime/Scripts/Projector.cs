using Unity.Mathematics;
using UnityEngine;

namespace Netherlands3D.Tilekit
{
    public abstract class Projector : ScriptableObject
    {
        public abstract float ToUnityUnits(double original);
        public abstract double FromUnityUnits(float original);

        public virtual Vector3 ToUnityUnits(double3 original)
        {
            return new Vector3(ToUnityUnits(original.x), ToUnityUnits(original.y), ToUnityUnits(original.z));
        }

        public virtual double3 FromUnityUnits(float3 original)
        {
            return new double3(
                FromUnityUnits(original.x), 
                FromUnityUnits(original.y),
                FromUnityUnits(original.z)
            );
        }

        public virtual Bounds ToUnityUnits(BoundsDouble original)
        {
            return new Bounds(
                ToUnityUnits(original.Center), 
                ToUnityUnits(original.Size)
            );
        }

        public virtual BoundsDouble FromUnityUnits(Bounds original)
        {
            return new BoundsDouble(
                FromUnityUnits(original.center), 
                FromUnityUnits(original.size)
            );
        }
    }
}