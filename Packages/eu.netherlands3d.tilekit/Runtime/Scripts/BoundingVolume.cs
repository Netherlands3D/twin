using System;
using Netherlands3D.Tilekit.Geometry;
using Netherlands3D.Tilekit.WriteModel;

namespace Netherlands3D.Tilekit
{
    /// <summary>
    /// Union type for all bounding volume types.
    /// </summary>
    public readonly struct BoundingVolume
    {
        private readonly TileSet store;
        private readonly int index;
        
        public BoundingVolume(TileSet store, int index)
        {
            this.store = store;
            this.index = index;
        }
        
        public BoxBoundingVolume AsBox() => store.BoundingVolumes.Box(index);
        public RegionBoundingVolume AsRegion() => store.BoundingVolumes.Region(index);
        public SphereBoundingVolume AsSphere() => store.BoundingVolumes.Sphere(index);

        public BoundsDouble ToBounds() => store.BoundingVolumes.Bounds(index);
    }
}