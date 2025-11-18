using Netherlands3D.Tilekit.BoundingVolumes;
using NUnit.Framework;
using Unity.Mathematics;

namespace Netherlands3D.Tilekit.Tests.BoundingVolumes
{
    public class BoxBoundingVolumeTests
    {
        [Test]
        public void SizeTopLeftBottomRight_FromOrthogonalHalfAxes_AreConsistent()
        {
            // Arrange
            var center = new double3(10, 20, 30);
            var halfX  = new double3(2.5, 0, 0);
            var halfY  = new double3(0, 5.0, 0);
            var halfZ  = new double3(0, 0, 1.5);
            var box = new BoxBoundingVolume(center, halfX, halfY, halfZ);

            // Act
            var size = box.Size;
            var tl = box.TopLeft;
            var br = box.BottomRight;

            // Assert
            Assert.That(size, Is.EqualTo(new double3(5.0, 10.0, 3.0)));
            Assert.That(tl, Is.EqualTo(center - size * 0.5));
            Assert.That(br, Is.EqualTo(center + size * 0.5));
        }

        [Test]
        public void FromBounds_ProducesExpectedHalfAxes_AndRoundTrips()
        {
            // Arrange
            var center = new double3(0, 0, 0);
            var size   = new double3(8, 6, 2);

            // Act
            var box = BoxBoundingVolume.FromBounds(center, size);

            // Assert
            Assert.That(box.Size, Is.EqualTo(size));
            Assert.That(box.TopLeft, Is.EqualTo(new double3(-4, -3, -1)));
            Assert.That(box.BottomRight, Is.EqualTo(new double3(4, 3, 1)));
        }

        [Test]
        public void FromTopLeftAndBottomRight_CorrectlyComputesCenterAndSize()
        {
            // Arrange
            var tl = new double3(-10, -20, -5);
            var br = new double3(  2,   4,  7);

            // Act
            var box = BoxBoundingVolume.FromTopLeftAndBottomRight(tl, br);

            // Assert
            Assert.That(box.TopLeft, Is.EqualTo(tl));
            Assert.That(box.BottomRight, Is.EqualTo(br));
            Assert.That(box.Size, Is.EqualTo(math.abs(br - tl)));
            Assert.That(box.ToBounds().Center, Is.EqualTo((tl + br) * 0.5));
        }

        [Test]
        public void Subdivide2D_ProducesFourQuadrants_CoveringOriginalExtent()
        {
            // Arrange
            var box = BoxBoundingVolume.FromTopLeftAndBottomRight(
                new double3(0, 0, 0),
                new double3(8, 6, 2)
            );

            // Act
            var (tl, tr, br, bl) = box.Subdivide2D();

            // Assert — sizes
            Assert.That(tl.Size, Is.EqualTo(new double3(4, 3, 2)));
            Assert.That(tr.Size, Is.EqualTo(new double3(4, 3, 2)));
            Assert.That(br.Size, Is.EqualTo(new double3(4, 3, 2)));
            Assert.That(bl.Size, Is.EqualTo(new double3(4, 3, 2)));

            // Assert — coverage by min/max
            var min = box.TopLeft;
            var max = box.BottomRight;
            var mid = (min + max) * 0.5;

            Assert.That(tl.TopLeft,     Is.EqualTo(new double3(min.x, min.y, min.z)));
            Assert.That(tl.BottomRight, Is.EqualTo(new double3(mid.x, mid.y, max.z)));

            Assert.That(tr.TopLeft,     Is.EqualTo(new double3(mid.x, min.y, min.z)));
            Assert.That(tr.BottomRight, Is.EqualTo(new double3(max.x, mid.y, max.z)));

            Assert.That(br.TopLeft,     Is.EqualTo(new double3(mid.x, mid.y, min.z)));
            Assert.That(br.BottomRight, Is.EqualTo(new double3(max.x, max.y, max.z)));

            Assert.That(bl.TopLeft,     Is.EqualTo(new double3(min.x, mid.y, min.z)));
            Assert.That(bl.BottomRight, Is.EqualTo(new double3(mid.x, max.y, max.z)));

            // Assert — union equals original bounds (by min/max recomposition)
            var unionMin = new double3(
                math.min(math.min(tl.TopLeft.x, tr.TopLeft.x), math.min(br.TopLeft.x, bl.TopLeft.x)),
                math.min(math.min(tl.TopLeft.y, tr.TopLeft.y), math.min(br.TopLeft.y, bl.TopLeft.y)),
                math.min(math.min(tl.TopLeft.z, tr.TopLeft.z), math.min(br.TopLeft.z, bl.TopLeft.z))
            );
            var unionMax = new double3(
                math.max(math.max(tl.BottomRight.x, tr.BottomRight.x), math.max(br.BottomRight.x, bl.BottomRight.x)),
                math.max(math.max(tl.BottomRight.y, tr.BottomRight.y), math.max(br.BottomRight.y, bl.BottomRight.y)),
                math.max(math.max(tl.BottomRight.z, tr.BottomRight.z), math.max(br.BottomRight.z, bl.BottomRight.z))
            );

            Assert.That(unionMin, Is.EqualTo(min));
            Assert.That(unionMax, Is.EqualTo(max));
        }

        [Test]
        public void ToBounds_RoundTripsCenterAndSize()
        {
            // Arrange
            var box = BoxBoundingVolume.FromBounds(
                new double3(3, 5, 7), new double3(2, 4, 6));

            // Act
            var b = box.ToBounds();

            // Assert
            Assert.That(b.Center, Is.EqualTo(new double3(3, 5, 7)));
            Assert.That(b.Size,   Is.EqualTo(new double3(2, 4, 6)));
        }
    }
}