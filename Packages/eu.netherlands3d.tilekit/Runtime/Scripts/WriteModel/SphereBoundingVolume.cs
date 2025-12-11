using System;
using System.Runtime.InteropServices;
using Netherlands3D.Tilekit.Geometry;
using Unity.Mathematics;

namespace Netherlands3D.Tilekit.WriteModel
{
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct SphereBoundingVolume // 4 doubles = 32 B
    {
        public readonly double3 Center;
        public readonly double Radius;

        public SphereBoundingVolume(double3 center, double radius)
        {
            Center = center;
            Radius = radius;
        }

        public BoundsDouble ToBounds()
        {
            throw new NotImplementedException();
        }
    }
}