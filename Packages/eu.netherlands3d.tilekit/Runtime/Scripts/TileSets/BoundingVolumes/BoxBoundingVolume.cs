using System;
using Unity.Mathematics;

namespace Netherlands3D.Tilekit.TileSets.BoundingVolumes
{
    public struct BoxBoundingVolume: IBoundingVolume
    {
        public double3 Center { get; }
        public double3 Size { get; }

        public BoundsDouble ToBounds()
        {
            throw new NotImplementedException();
        }
    }
}