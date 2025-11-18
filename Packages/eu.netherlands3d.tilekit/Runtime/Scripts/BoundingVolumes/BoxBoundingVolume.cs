using System.Runtime.InteropServices;
using Netherlands3D.Tilekit.Geometry;
using Unity.Mathematics;

namespace Netherlands3D.Tilekit.BoundingVolumes
{
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct BoxBoundingVolume // 12 doubles = 96 B
    {
        public readonly double3 Center;
        public readonly double3 HalfAxisX;
        public readonly double3 HalfAxisY;
        public readonly double3 HalfAxisZ;

        /// <summary>
        /// Total box size derived from the magnitude of the half axes.
        /// </summary>
        public double3 Size => new(math.length(HalfAxisX) * 2.0, math.length(HalfAxisY) * 2.0, math.length(HalfAxisZ) * 2.0);

        /// <summary>
        /// The corner with the smallest x/y/z values.
        /// </summary>
        public double3 TopLeft => Center - Size * 0.5;

        /// <summary>
        /// The corner with the largest x/y/z values.
        /// </summary>
        public double3 BottomRight => Center + Size * 0.5;

        public BoxBoundingVolume(double3 center, double3 halfAxisX, double3 halfAxisY, double3 halfAxisZ)
        {
            Center = center;
            HalfAxisX = halfAxisX;
            HalfAxisY = halfAxisY;
            HalfAxisZ = halfAxisZ;
        }
        
        /// <summary>
        /// Creates a box centered at <paramref name="center"/> with the given <paramref name="size"/>.
        /// </summary>
        public static BoxBoundingVolume FromBounds(double3 center, double3 size)
        {
            return new BoxBoundingVolume(
                center,
                new double3(size.x * 0.5, 0, 0),
                new double3(0, size.y * 0.5, 0),
                new double3(0, 0, size.z * 0.5)
            );
        }
        
        /// <summary>
        /// Creates a box from top-left and bottom-right coordinates (as double3).
        /// </summary>
        public static BoxBoundingVolume FromTopLeftAndBottomRight(double3 topLeft, double3 bottomRight)
        {
            return FromBounds(
                (topLeft + bottomRight) * 0.5, 
                math.abs(bottomRight - topLeft)
            );
        }

        public BoundsDouble ToBounds()
        {
            return new BoundsDouble(Center, Size);
        }

        public (BoxBoundingVolume tl, BoxBoundingVolume tr, BoxBoundingVolume br, BoxBoundingVolume bl) Subdivide2D()
        {
            var min = TopLeft;
            var max = BottomRight;

            var midX = (min.x + max.x) * 0.5;
            var midY = (min.y + max.y) * 0.5;

            var tl = FromTopLeftAndBottomRight(new double3(min.x, min.y, min.z), new double3(midX, midY, max.z));
            var tr = FromTopLeftAndBottomRight(new double3(midX, min.y, min.z), new double3(max.x, midY, max.z));
            var br = FromTopLeftAndBottomRight(new double3(midX, midY, min.z), new double3(max.x, max.y, max.z));
            var bl = FromTopLeftAndBottomRight(new double3(min.x, midY, min.z), new double3(midX, max.y, max.z));

            return (tl, tr, br, bl);
        }
    }
}