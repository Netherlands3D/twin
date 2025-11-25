using Netherlands3D.Tilekit.WriteModel;
using NUnit.Framework;
using Unity.Collections;
using Unity.Mathematics;

namespace Netherlands3D.Tilekit.Tests.BoundingVolumes
{
    public class BoundingVolumeStoreTests
    {
        [Test]
        public void Alloc_CreatesNativeArraysOfGivenSize()
        {
            // Arrange
            var store = new BoundingVolumeStore();

            // Act
            store.Alloc(initialSize: 8, alloc: Allocator.Temp);

            // Assert
            Assert.That(store.BoundingVolumeRefs.IsCreated, Is.True);
            Assert.That(store.Boxes.IsCreated, Is.True);
            Assert.That(store.Regions.IsCreated, Is.True);
            Assert.That(store.Spheres.IsCreated, Is.True);

            Assert.That(store.BoundingVolumeRefs.Length, Is.EqualTo(8));
            Assert.That(store.Boxes.Length, Is.EqualTo(8));
            Assert.That(store.Regions.Length, Is.EqualTo(8));
            Assert.That(store.Spheres.Length, Is.EqualTo(8));

            // Cleanup
            store.BoundingVolumeRefs.Dispose();
            store.Boxes.Dispose();
            store.Regions.Dispose();
            store.Spheres.Dispose();
        }

        [Test]
        public void Add_Box_WritesBoxAndRef()
        {
            // Arrange
            var store = new BoundingVolumeStore();
            store.Alloc(4, Allocator.Temp);

            var box = BoxBoundingVolume.FromBounds(new double3(0,0,0), new double3(2,4,6));

            // Act
            var @ref = store.Add(2, box);

            // Assert
            Assert.That(store.Boxes[2].Size, Is.EqualTo(new double3(2,4,6)));
            Assert.That(store.BoundingVolumeRefs[2].Type, Is.EqualTo(BoundingVolumeType.Box));
            Assert.That(store.BoundingVolumeRefs[2].Index, Is.EqualTo(2));
            Assert.That(@ref.Type, Is.EqualTo(BoundingVolumeType.Box));
            Assert.That(@ref.Index, Is.EqualTo(2));

            // Cleanup
            store.BoundingVolumeRefs.Dispose();
            store.Boxes.Dispose();
            store.Regions.Dispose();
            store.Spheres.Dispose();
        }

        [Test]
        public void Add_Sphere_WritesSphereAndRef()
        {
            // Arrange
            var store = new BoundingVolumeStore();
            store.Alloc(3, Allocator.Temp);

            var s = new SphereBoundingVolume(new double3(1,2,3), 9.5);

            // Act
            var @ref = store.Add(1, s);

            // Assert
            Assert.That(store.Spheres[1].Center, Is.EqualTo(new double3(1,2,3)));
            Assert.That(store.Spheres[1].Radius, Is.EqualTo(9.5));
            Assert.That(store.BoundingVolumeRefs[1].Type, Is.EqualTo(BoundingVolumeType.Sphere));
            Assert.That(store.BoundingVolumeRefs[1].Index, Is.EqualTo(1));
            Assert.That(@ref.Type, Is.EqualTo(BoundingVolumeType.Sphere));
            Assert.That(@ref.Index, Is.EqualTo(1));

            // Cleanup
            store.BoundingVolumeRefs.Dispose();
            store.Boxes.Dispose();
            store.Regions.Dispose();
            store.Spheres.Dispose();
        }

        [Test]
        public void Add_Region_WritesRegionAndRef()
        {
            // Arrange
            var store = new BoundingVolumeStore();
            store.Alloc(2, Allocator.Temp);

            var r = new RegionBoundingVolume(1,2,3,4,5,6);

            // Act
            var @ref = store.Add(0, r);

            // Assert
            Assert.That(store.Regions[0].West,  Is.EqualTo(1));
            Assert.That(store.Regions[0].South, Is.EqualTo(2));
            Assert.That(store.Regions[0].East,  Is.EqualTo(3));
            Assert.That(store.Regions[0].North, Is.EqualTo(4));
            Assert.That(store.Regions[0].MinHeight, Is.EqualTo(5));
            Assert.That(store.Regions[0].MaxHeight, Is.EqualTo(6));

            Assert.That(store.BoundingVolumeRefs[0].Type, Is.EqualTo(BoundingVolumeType.Region));
            Assert.That(store.BoundingVolumeRefs[0].Index, Is.EqualTo(0));
            Assert.That(@ref.Type, Is.EqualTo(BoundingVolumeType.Region));
            Assert.That(@ref.Index, Is.EqualTo(0));

            // Cleanup
            store.BoundingVolumeRefs.Dispose();
            store.Boxes.Dispose();
            store.Regions.Dispose();
            store.Spheres.Dispose();
        }

        [Test]
        public void Add_OverwritesDefaultUninitializedRef()
        {
            // Arrange
            var store = new BoundingVolumeStore();
            store.Alloc(1, Allocator.Temp);

            // Act
            // Before add, default ref.Type should be Uninitialized
            Assert.That(store.BoundingVolumeRefs[0].Type, Is.EqualTo(BoundingVolumeType.Uninitialized));

            var box = BoxBoundingVolume.FromBounds(new double3(0,0,0), new double3(1,1,1));
            store.Add(0, box);

            // Assert
            Assert.That(store.BoundingVolumeRefs[0].Type, Is.EqualTo(BoundingVolumeType.Box));

            // Cleanup
            store.BoundingVolumeRefs.Dispose();
            store.Boxes.Dispose();
            store.Regions.Dispose();
            store.Spheres.Dispose();
        }

        [Test]
        public void Add_IndexOutOfRange_Throws()
        {
            // Arrange
            var store = new BoundingVolumeStore();
            store.Alloc(1, Allocator.Temp);

            var box = BoxBoundingVolume.FromBounds(new double3(0,0,0), new double3(1,1,1));
            var sph = new SphereBoundingVolume(new double3(0,0,0), 1);
            var reg = new RegionBoundingVolume(0,0,0,0,0,0);

            // Act + Assert
            Assert.That(() => store.Add(1, box), Throws.Exception);
            Assert.That(() => store.Add(2, sph), Throws.Exception);
            Assert.That(() => store.Add(3, reg), Throws.Exception);

            // Cleanup
            store.BoundingVolumeRefs.Dispose();
            store.Boxes.Dispose();
            store.Regions.Dispose();
            store.Spheres.Dispose();
        }
    }
}