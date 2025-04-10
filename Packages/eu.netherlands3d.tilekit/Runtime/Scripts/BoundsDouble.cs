using System;
using Unity.Mathematics;
using UnityEngine;

namespace Netherlands3D.Tilekit
{
    [Serializable]
    public struct BoundsDouble
    {
        public double3 Center { get; private set; }
        public double3 Size { get; private set; }

        public double3 Extents => Size * 0.5;

        public double3 Min => Center - Extents;
        public double3 Max => Center + Extents;

        public BoundsDouble(double3 center, double3 size)
        {
            Center = center;
            Size = size;
        }

        public void SetMinMax(double3 min, double3 max)
        {
            Size = max - min;
            Center = min + Extents;
        }

        public bool Contains(double3 point)
        {
            double3 min = Min;
            double3 max = Max;

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

        public void Encapsulate(double3 point)
        {
            double3 min = math.min(Min, point);
            double3 max = math.max(Max, point);

            SetMinMax(min, max);
        }

        public void Encapsulate(BoundsDouble other)
        {
            Encapsulate(other.Min);
            Encapsulate(other.Max);
        }

        public void Expand(double amount)
        {
            Size += new double3(amount, amount, amount);
        }

        public void Expand(double3 amount)
        {
            Size += amount;
        }

        public override string ToString()
        {
            return $"Center: {Center}, Size: {Size}";
        }
        
        public double3 ClosestPoint(double3 point)
        {
            double3 min = Min;
            double3 max = Max;

            double3 closestPoint = new double3(
                math.clamp(point.x, min.x, max.x),
                math.clamp(point.y, min.y, max.y),
                math.clamp(point.z, min.z, max.z)
            );

            return closestPoint;
        }

        public static implicit operator Bounds(BoundsDouble boundsDouble)
        {
            return new Bounds(
                new Vector3(
                    (float)boundsDouble.Center.x,
                    (float)boundsDouble.Center.y,
                    (float)boundsDouble.Center.z
                ), 
                new Vector3(
                    (float)boundsDouble.Size.x,
                    (float)boundsDouble.Size.y,
                    (float)boundsDouble.Size.z
                ) 
            );
        }

        public static implicit operator BoundsDouble(Bounds unityBounds)
        {
            return new BoundsDouble(
                new double3(unityBounds.center.x, unityBounds.center.y, unityBounds.center.z), 
                new double3(unityBounds.size.x, unityBounds.size.y, unityBounds.size.z)
            );
        }
    }
}