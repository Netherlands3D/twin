using System;
using Netherlands3D.Tilekit.Geometry;
using Netherlands3D.Tilekit.MemoryManagement;
using Unity.Collections;

namespace Netherlands3D.Tilekit.WriteModel
{
    // TODO: Change to a double buffer instead of structs - this way we can be a lot more compact
    public sealed class BoundingVolumeStore : IDisposable, IMemoryReporter
    {
        private NativeArray<BoundingVolumeRef> boundingVolumeRefs;
        private NativeArray<BoxBoundingVolume> boxes;
        private NativeArray<RegionBoundingVolume> regions;
        private NativeArray<SphereBoundingVolume> spheres;

        public BoundingVolumeStore(int initialSize, Allocator alloc = Allocator.Persistent)
        {
            boundingVolumeRefs = new NativeArray<BoundingVolumeRef>(initialSize, alloc);
            boxes = new NativeArray<BoxBoundingVolume>(initialSize, alloc);
            regions = new NativeArray<RegionBoundingVolume>(initialSize, alloc);
            spheres = new NativeArray<SphereBoundingVolume>(initialSize, alloc);
        }
        
        public BoundingVolumeRef Add(int idx, BoxBoundingVolume b)
        {
            boxes[idx] = b;
            boundingVolumeRefs[idx] = new BoundingVolumeRef(BoundingVolumeType.Box, idx);
            return boundingVolumeRefs[idx];
        }

        public BoundingVolumeRef Add(int idx, RegionBoundingVolume r)
        {
            regions[idx] = r;
            boundingVolumeRefs[idx] = new BoundingVolumeRef(BoundingVolumeType.Region, idx);
            return boundingVolumeRefs[idx];
        }

        public BoundingVolumeRef Add(int idx, SphereBoundingVolume s)
        {
            spheres[idx] = s;
            boundingVolumeRefs[idx] = new BoundingVolumeRef(BoundingVolumeType.Sphere, idx);
            return boundingVolumeRefs[idx];
        }

        public BoxBoundingVolume Box(int index) => boxes[boundingVolumeRefs[index].Index];

        public RegionBoundingVolume Region(int index) => regions[boundingVolumeRefs[index].Index];

        public SphereBoundingVolume Sphere(int index) => spheres[boundingVolumeRefs[index].Index];

        public BoundsDouble Bounds(int index)
        {
            var boundingVolumeRef = boundingVolumeRefs[index];
            return boundingVolumeRef.Type switch
            {
                BoundingVolumeType.Region => Region(boundingVolumeRef.Index).ToBounds(),
                BoundingVolumeType.Box => Box(boundingVolumeRef.Index).ToBounds(),
                BoundingVolumeType.Sphere => Sphere(boundingVolumeRef.Index).ToBounds(),
                _ => throw new Exception("Invalid bounding volume type")
            };
        }

        public long GetReservedBytes() 
            => boundingVolumeRefs.GetReservedBytes() + boxes.GetReservedBytes() + regions.GetReservedBytes() + spheres.GetReservedBytes();

        public long GetUsedBytes() 
            => boundingVolumeRefs.GetUsedBytes() + boxes.GetUsedBytes() + regions.GetUsedBytes() + spheres.GetUsedBytes();

        public void Dispose()
        {
            boundingVolumeRefs.Dispose();
            boxes.Dispose();
            regions.Dispose();
            spheres.Dispose();
        }
    }
}