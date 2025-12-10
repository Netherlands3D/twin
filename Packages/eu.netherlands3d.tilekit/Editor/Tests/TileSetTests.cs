using System;
using Netherlands3D.Tilekit.WriteModel;
using NUnit.Framework;
using Unity.Collections;
using Unity.Mathematics;

namespace Netherlands3D.Tilekit.Tests
{
    [TestFixture]
    public class TileSetTests
    {
        private TileSet s;
        private static BoxBoundingVolume AreaOfInterest
        {
            get
            {
                int left = 153000;
                int right = 158000;
                int top = 462000;
                int bottom = 467000;

                var areaOfInterest = BoxBoundingVolume.FromTopLeftAndBottomRight(
                    new double3(left, top, 0),
                    new double3(right, bottom, 0)
                );
                return areaOfInterest;
            }
        }
        
        [SetUp]
        public void SetUp()
        {
            s = new TileSet(AreaOfInterest, initialSize: 64, alloc: Allocator.Temp);
        }

        [TearDown]
        public void TearDown()
        {
            s.Dispose();
        }

        static BoxBoundingVolume Box() =>
            BoxBoundingVolume.FromBounds(new double3(0, 0, 0), new double3(2, 2, 2));

        [Test]
        public void Children_ForFirstTileWithOffsetZero_MatchesAddedIds()
        {
            // Arrange
            int a = s.AddTile(Box(), 1.0, ReadOnlySpan<TileContentData>.Empty, new int[] { 1, 2, 3, 4 });
            int _1 = s.AddTile(Box(), 1.0, ReadOnlySpan<TileContentData>.Empty, ReadOnlySpan<int>.Empty);
            int _2 = s.AddTile(Box(), 1.0, ReadOnlySpan<TileContentData>.Empty, ReadOnlySpan<int>.Empty);
            int _3 = s.AddTile(Box(), 1.0, ReadOnlySpan<TileContentData>.Empty, ReadOnlySpan<int>.Empty);
            int _4 = s.AddTile(Box(), 1.0, ReadOnlySpan<TileContentData>.Empty, ReadOnlySpan<int>.Empty);

            var tile = s.GetTile(a);

            // Act
            var children = tile.Children();

            // Assert
            Assert.That(children.Length, Is.EqualTo(4));
            Assert.That(children[0], Is.EqualTo(1));
            Assert.That(children[1], Is.EqualTo(2));
            Assert.That(children[2], Is.EqualTo(3));
            Assert.That(children[3], Is.EqualTo(4));

            // Sanity via GetChild (this will fail if offset * i is used)
            Assert.That(tile.GetChild(0).Index, Is.EqualTo(1));
            Assert.That(tile.GetChild(1).Index, Is.EqualTo(2));
            Assert.That(tile.GetChild(2).Index, Is.EqualTo(3));
            Assert.That(tile.GetChild(3).Index, Is.EqualTo(4));
        }

        [Test]
        public void Children_ForTileWithNonZeroOffset_UsesAdditionNotMultiplication()
        {
            // Arrange
            // First tile has 3 kids to push the Flat offset to 3
            int t0 = s.AddTile(Box(), 1.0, ReadOnlySpan<TileContentData>.Empty, new int[] { 10, 11, 12 });

            // Add actual child tiles (ids will be 1..)
            int c0 = s.AddTile(Box(), 1.0, ReadOnlySpan<TileContentData>.Empty, ReadOnlySpan<int>.Empty);
            int c1 = s.AddTile(Box(), 1.0, ReadOnlySpan<TileContentData>.Empty, ReadOnlySpan<int>.Empty);
            int c2 = s.AddTile(Box(), 1.0, ReadOnlySpan<TileContentData>.Empty, ReadOnlySpan<int>.Empty);
            int c3 = s.AddTile(Box(), 1.0, ReadOnlySpan<TileContentData>.Empty, ReadOnlySpan<int>.Empty);

            // Second parent’s children start at Flat offset 3 (non-zero)
            int t1 = s.AddTile(Box(), 1.0, ReadOnlySpan<TileContentData>.Empty, new int[] { c0, c1, c2, c3 });
            var tile = s.GetTile(t1);

            // Act
            var r = s.Children.Ranges[t1];
            var bucket = tile.Children();

            // Assert offsets and count first
            Assert.That(r.Offset, Is.EqualTo(3));
            Assert.That(r.Count, Is.EqualTo(4));
            Assert.That(r.Offset + r.Count, Is.LessThanOrEqualTo(s.Children.Items.Length));

            // Direct from bucket
            Assert.That(bucket.Length, Is.EqualTo(4));
            Assert.That(bucket[0], Is.EqualTo(c0));
            Assert.That(bucket[1], Is.EqualTo(c1));
            Assert.That(bucket[2], Is.EqualTo(c2));
            Assert.That(bucket[3], Is.EqualTo(c3));

            // Through GetChild — this specifically catches offset*i bugs
            Assert.That(tile.GetChild(0).Index, Is.EqualTo(c0));
            Assert.That(tile.GetChild(1).Index, Is.EqualTo(c1));
            Assert.That(tile.GetChild(2).Index, Is.EqualTo(c2));
            Assert.That(tile.GetChild(3).Index, Is.EqualTo(c3));
        }

        [Test]
        public void Children_EmptyBucket_IsSafeAndGetChildThrows()
        {
            // Arrange
            int t = s.AddTile(Box(), 1.0, ReadOnlySpan<TileContentData>.Empty, ReadOnlySpan<int>.Empty);
            var tile = s.GetTile(t);

            // Act
            var children = tile.Children();

            // Assert
            Assert.That(children.Length, Is.EqualTo(0));
            Assert.That(() => tile.GetChild(0), Throws.Exception);
        }

        [Test]
        public void RangesAndFlat_StayConsistentAcrossMultipleAdds()
        {
            // Arrange
            int t0 = s.AddTile(Box(), 1.0, ReadOnlySpan<TileContentData>.Empty, new int[] { 2, 4 });
            int t1 = s.AddTile(Box(), 1.0, ReadOnlySpan<TileContentData>.Empty, new int[] { 7 });
            int t2 = s.AddTile(Box(), 1.0, ReadOnlySpan<TileContentData>.Empty, new int[] { 9, 11, 13 });

            // Act
            var r0 = s.Children.Ranges[t0];
            var r1 = s.Children.Ranges[t1];
            var r2 = s.Children.Ranges[t2];

            // Assert
            Assert.That(r0.Offset, Is.EqualTo(0));
            Assert.That(r0.Count, Is.EqualTo(2));
            Assert.That(s.Children.Items[r0.Offset + 0], Is.EqualTo(2));
            Assert.That(s.Children.Items[r0.Offset + 1], Is.EqualTo(4));

            Assert.That(r1.Offset, Is.EqualTo(2));
            Assert.That(r1.Count, Is.EqualTo(1));
            Assert.That(s.Children.Items[r1.Offset + 0], Is.EqualTo(7));

            Assert.That(r2.Offset, Is.EqualTo(3));
            Assert.That(r2.Count, Is.EqualTo(3));
            Assert.That(s.Children.Items[r2.Offset + 0], Is.EqualTo(9));
            Assert.That(s.Children.Items[r2.Offset + 1], Is.EqualTo(11));
            Assert.That(s.Children.Items[r2.Offset + 2], Is.EqualTo(13));

            // Flat length must equal the sum of counts
            Assert.That(s.Children.Items.Length, Is.EqualTo(r0.Count + r1.Count + r2.Count));
        }

        [Test]
        public void TileChildren_EnumerationEquivalentToGetChild()
        {
            // Arrange
            int t = s.AddTile(Box(), 1.0, ReadOnlySpan<TileContentData>.Empty, new int[] { 5, 6, 7, 8 });
            // Ensure child ids actually exist as tiles
            s.AddTile(Box(), 1.0, ReadOnlySpan<TileContentData>.Empty, ReadOnlySpan<int>.Empty); // id 1 or more—don’t rely on exact ids here
            s.AddTile(Box(), 1.0, ReadOnlySpan<TileContentData>.Empty, ReadOnlySpan<int>.Empty);
            s.AddTile(Box(), 1.0, ReadOnlySpan<TileContentData>.Empty, ReadOnlySpan<int>.Empty);
            s.AddTile(Box(), 1.0, ReadOnlySpan<TileContentData>.Empty, ReadOnlySpan<int>.Empty);

            var tile = s.GetTile(t);

            // Act
            var bucket = tile.Children();

            // Assert
            for (int i = 0; i < bucket.Length; i++)
            {
                Assert.That(bucket[i], Is.EqualTo(tile.GetChild(i).Index));
            }
        }

        [Test]
        public void ContentsAndChildren_DoNotInterfere_WithEachOtherOffsets()
        {
            // Arrange
            // Add some contents to shift contents’ flat buffer; ensure children offsets are still right.
            var uriIdx = 0; // string table index not used here
            var content = new TileContentData(uriIdx, new BoundingVolumeRef(BoundingVolumeType.Box, 0));

            int t0 = s.AddTile(Box(), 1.0, new TileContentData[] { content, content }, new int[] { 3, 4 });
            int t1 = s.AddTile(Box(), 1.0, new TileContentData[] { content }, new int[] { 7 });
            int t2 = s.AddTile(Box(), 1.0, new TileContentData[] { content, content, content }, new int[] { 9, 10, 11 });

            // Act
            var r0 = s.Children.Ranges[t0];
            var r1 = s.Children.Ranges[t1];
            var r2 = s.Children.Ranges[t2];

            // Assert (children side unaffected by contents packing)
            Assert.That(s.Children.Items[r0.Offset + 0], Is.EqualTo(3));
            Assert.That(s.Children.Items[r0.Offset + 1], Is.EqualTo(4));
            Assert.That(s.Children.Items[r1.Offset + 0], Is.EqualTo(7));
            Assert.That(s.Children.Items[r2.Offset + 0], Is.EqualTo(9));
            Assert.That(s.Children.Items[r2.Offset + 1], Is.EqualTo(10));
            Assert.That(s.Children.Items[r2.Offset + 2], Is.EqualTo(11));
        }

        [Test]
        public void OutOfRange_GetChild_Throws()
        {
            // Arrange
            int t = s.AddTile(Box(), 1.0, ReadOnlySpan<TileContentData>.Empty, new int[] { 1, 2, 3 });
            var tile = s.GetTile(t);

            // Act + Assert
            Assert.That(() => tile.GetChild(-1), Throws.Exception);
            Assert.That(() => tile.GetChild(3), Throws.Exception); // Count == 3, last valid index is 2
        }
    }
}