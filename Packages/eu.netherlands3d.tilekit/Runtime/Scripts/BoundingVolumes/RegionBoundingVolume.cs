using System;
using System.Runtime.InteropServices;
using Netherlands3D.Tilekit.Geometry;

namespace Netherlands3D.Tilekit.BoundingVolumes
{
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct RegionBoundingVolume // 6 doubles = 48 B
    {
        public readonly double West, South, East, North, MinHeight, MaxHeight;

        public RegionBoundingVolume(double west, double south, double east, double north, double minHeight, double maxHeight)
        {
            West = west;
            South = south;
            East = east;
            North = north;
            MinHeight = minHeight;
            MaxHeight = maxHeight;
        }

        public BoundsDouble ToBounds()
        {
            throw new NotImplementedException();
        }
    }
}