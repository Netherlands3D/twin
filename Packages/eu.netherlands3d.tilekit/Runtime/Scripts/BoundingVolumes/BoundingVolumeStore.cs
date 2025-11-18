using Unity.Collections;

namespace Netherlands3D.Tilekit.BoundingVolumes
{
    public struct BoundingVolumeStore
    {
        // TODO: Change to native lists
        public NativeArray<BoundingVolumeRef> BoundingVolumeRefs;
        public NativeArray<BoxBoundingVolume> Boxes;
        public NativeArray<RegionBoundingVolume> Regions;
        public NativeArray<SphereBoundingVolume> Spheres;

        public void Alloc(int initialSize, Allocator alloc = Allocator.Persistent)
        {
            BoundingVolumeRefs = new NativeArray<BoundingVolumeRef>(initialSize, alloc);
            Boxes = new NativeArray<BoxBoundingVolume>(initialSize, alloc);
            Regions = new NativeArray<RegionBoundingVolume>(initialSize, alloc);
            Spheres = new NativeArray<SphereBoundingVolume>(initialSize, alloc);
        }
        
        public BoundingVolumeRef Add(int idx, BoxBoundingVolume b)
        {
            Boxes[idx] = b;
            BoundingVolumeRefs[idx] = new BoundingVolumeRef(BoundingVolumeType.Box, idx);
            return BoundingVolumeRefs[idx];
        }

        public BoundingVolumeRef Add(int idx, RegionBoundingVolume r)
        {
            Regions[idx] = r;
            BoundingVolumeRefs[idx] = new BoundingVolumeRef(BoundingVolumeType.Region, idx);
            return BoundingVolumeRefs[idx];
        }

        public BoundingVolumeRef Add(int idx, SphereBoundingVolume s)
        {
            Spheres[idx] = s;
            BoundingVolumeRefs[idx] = new BoundingVolumeRef(BoundingVolumeType.Sphere, idx);
            return BoundingVolumeRefs[idx];
        }
    }
}