using Unity.Mathematics;

namespace Netherlands3D.Tilekit.TileSets
{
    public interface IBoundingVolume
    {
        public double3 Center { get; }
        public double3 Size { get; }

        public BoundsDouble ToBounds();
    }
}