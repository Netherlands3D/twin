using UnityEngine;

namespace Netherlands3D.Tilekit
{
    public abstract class Projector : ScriptableObject
    {
        public abstract float ToUnityUnits(double original);
        public abstract double FromUnityUnits(float original);

        public virtual Vector3 ToUnityUnits(Vector3Double original)
        {
            return new Vector3(ToUnityUnits(original.x), ToUnityUnits(original.y), ToUnityUnits(original.z));
        }

        public virtual Vector3Double FromUnityUnits(Vector3 original)
        {
            return new Vector3Double(
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
                ToUnityUnits(original.center), 
                ToUnityUnits(original.size)
            );
        }
    }
}