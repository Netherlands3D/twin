using System;
using System.Runtime.Serialization;
using Unity.Mathematics;

namespace Netherlands3D.Tilekit.TileSets.BoundingVolumes
{
    [DataContract(Namespace = "eu.netherlands3d.tilekit.tilesets.bounding_volumes", Name = "SphereBoundingVolume")]
    public struct SphereBoundingVolume : IBoundingVolume
    {
        public double3 Center { get; }
        public double3 Size { get; }

        public BoundsDouble ToBounds()
        {
            throw new NotImplementedException();
        }
    }
}