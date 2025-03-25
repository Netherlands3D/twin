using System;
using UnityEngine;

namespace Netherlands3D.Tilekit
{
    [Serializable]
    public class BoundsDouble
    {
        public Vector3Double Center { get; private set; }
        public Vector3Double Size { get; private set; }

        public Vector3Double Extents => Size * 0.5;

        public Vector3Double Min => Center - Extents;
        public Vector3Double Max => Center + Extents;

        public BoundsDouble(Vector3Double center, Vector3Double size)
        {
            Center = center;
            Size = size;
        }

        public void SetMinMax(Vector3Double min, Vector3Double max)
        {
            Size = max - min;
            Center = min + Extents;
        }

        public bool Contains(Vector3Double point)
        {
            Vector3Double min = Min;
            Vector3Double max = Max;

            return (point.x >= min.x && point.x <= max.x) &&
                   (point.y >= min.y && point.y <= max.y) &&
                   (point.z >= min.z && point.z <= max.z);
        }

        public bool Intersects(BoundsDouble other)
        {
            return (Min.x <= other.Max.x && Max.x >= other.Min.x) &&
                   (Min.y <= other.Max.y && Max.y >= other.Min.y) &&
                   (Min.z <= other.Max.z && Max.z >= other.Min.z);
        }

        public void Encapsulate(Vector3Double point)
        {
            Vector3Double min = Min;
            Vector3Double max = Max;

            min = Vector3Double.Min(min, point);
            max = Vector3Double.Max(max, point);

            SetMinMax(min, max);
        }

        public void Encapsulate(BoundsDouble other)
        {
            Encapsulate(other.Min);
            Encapsulate(other.Max);
        }

        public void Expand(double amount)
        {
            Size += new Vector3Double(amount, amount, amount);
        }

        public void Expand(Vector3Double amount)
        {
            Size += amount;
        }

        public override string ToString()
        {
            return $"Center: {Center}, Size: {Size}";
        }
        
        public static implicit operator Bounds(BoundsDouble boundsDouble)
        {
            return new Bounds(boundsDouble.Center, boundsDouble.Size);
        }

        public static implicit operator BoundsDouble(Bounds unityBounds)
        {
            return new BoundsDouble(unityBounds.center, unityBounds.size);
        }
    }
}