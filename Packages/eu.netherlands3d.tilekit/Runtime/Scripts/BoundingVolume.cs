using System;
using Netherlands3D.Tilekit.BoundingVolumes;
using Netherlands3D.Tilekit.Geometry;
using Netherlands3D.Tilekit.WriteModel;

namespace Netherlands3D.Tilekit
{
    /// <summary>
    /// Union type for all bounding volume types.
    /// </summary>
    public readonly struct BoundingVolume
    {
        private readonly ColdStorage store;
        private readonly int index;
        
        public BoundingVolume(ColdStorage store, int index)
        {
            this.store = store;
            this.index = index;
        }
        
        public BoxBoundingVolume AsBox() => store.BoundingVolumes.Boxes[index];
        public RegionBoundingVolume AsRegion() => store.BoundingVolumes.Regions[index];
        public SphereBoundingVolume AsSphere() => store.BoundingVolumes.Spheres[index];

        public BoundsDouble ToBounds()
        {
            var boundingVolumeRef = store.BoundingVolumes.BoundingVolumeRefs[index];
            return boundingVolumeRef.Type switch
            {
                BoundingVolumeType.Region => AsRegion().ToBounds(),
                BoundingVolumeType.Box => AsBox().ToBounds(),
                BoundingVolumeType.Sphere => AsSphere().ToBounds(),
                _ => throw new Exception("Invalid bounding volume type")
            };
        }
    }
}