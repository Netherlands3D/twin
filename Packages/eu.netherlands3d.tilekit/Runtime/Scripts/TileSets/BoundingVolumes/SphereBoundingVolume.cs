using System;
using System.Runtime.Serialization;

namespace Netherlands3D.Tilekit.TileSets.BoundingVolumes
{
    [DataContract(Namespace = "eu.netherlands3d.tilekit.tilesets.bounding_volumes", Name = "SphereBoundingVolume")]
    public class SphereBoundingVolume : BoundingVolume
    {
        public override BoundsDouble ToBounds()
        {
            throw new NotImplementedException();
        }
    }
}