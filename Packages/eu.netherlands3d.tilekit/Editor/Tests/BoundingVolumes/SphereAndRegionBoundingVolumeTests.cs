using Netherlands3D.Tilekit.WriteModel;
using NUnit.Framework;
using Unity.Mathematics;

namespace Netherlands3D.Tilekit.Tests.BoundingVolumes
{
    public class SphereAndRegionBoundingVolumeTests
    {
        [Test]
        public void Sphere_Ctor_StoresCenterAndRadius()
        {
            // Arrange
            var c = new double3(1.25, -3.5, 7);

            // Act
            var s = new SphereBoundingVolume(c, 12.5);

            // Assert
            Assert.That(s.Center, Is.EqualTo(c));
            Assert.That(s.Radius, Is.EqualTo(12.5));
        }

        [Test]
        public void Region_Ctor_StoresValues()
        {
            // Arrange

            // Act
            var r = new RegionBoundingVolume(west: 1, south: 2, east: 3, north: 4, minHeight: -10, maxHeight: 250);

            // Assert
            Assert.That(r.West, Is.EqualTo(1));
            Assert.That(r.South, Is.EqualTo(2));
            Assert.That(r.East, Is.EqualTo(3));
            Assert.That(r.North, Is.EqualTo(4));
            Assert.That(r.MinHeight, Is.EqualTo(-10));
            Assert.That(r.MaxHeight, Is.EqualTo(250));
        }

        [Test]
        public void Sphere_ToBounds_NotImplemented_Throws()
        {
            // Arrange
            var s = new SphereBoundingVolume(new double3(0,0,0), 5);

            // Act + Assert
            Assert.That(() => s.ToBounds(), Throws.TypeOf<System.NotImplementedException>());
        }

        [Test]
        public void Region_ToBounds_NotImplemented_Throws()
        {
            // Arrange
            var r = new RegionBoundingVolume(0, 0, 1, 1, 0, 10);

            // Act + Assert
            Assert.That(() => r.ToBounds(), Throws.TypeOf<System.NotImplementedException>());
        }
    }
}