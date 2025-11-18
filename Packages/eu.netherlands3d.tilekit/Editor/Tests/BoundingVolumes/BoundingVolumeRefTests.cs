using Netherlands3D.Tilekit.BoundingVolumes;
using NUnit.Framework;

namespace Netherlands3D.Tilekit.Tests.BoundingVolumes
{
    public class BoundingVolumeRefTests
    {
        [Test]
        public void Ctor_SetsTypeAndIndex()
        {
            // Arrange

            // Act
            var r = new BoundingVolumeRef(BoundingVolumeType.Box, 42);

            // Assert
            Assert.That(r.Type, Is.EqualTo(BoundingVolumeType.Box));
            Assert.That(r.Index, Is.EqualTo(42));
        }
    }
}