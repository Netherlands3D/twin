using System;
using Netherlands3D.Tilekit.BoundingVolumes;
using Netherlands3D.Tilekit.WriteModel;
using NUnit.Framework;
using Unity.Collections;
using Unity.Mathematics;

namespace Netherlands3D.Tilekit.Tests
{
    [TestFixture]
    public class TileTests
    {
        private ColdStorage s;

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
            s = new ColdStorage(AreaOfInterest, initialSize: 64, alloc: Allocator.Temp);
        }

        [TearDown]
        public void TearDown()
        {
            s.Dispose();
        }

        static BoxBoundingVolume Box() =>
            BoxBoundingVolume.FromBounds(new double3(0, 0, 0), new double3(2, 2, 2));

        [Test]
        public void Tile_Exposes_SoA_Fields()
        {
            // Arrange
            var tr = float4x4.Scale(new float3(2, 3, 4));
            int id = s.AddTile(
                Box(),
                geometricError: 7.5,
                contents: ReadOnlySpan<TileContentData>.Empty,
                children: ReadOnlySpan<int>.Empty,
                refine: MethodOfRefinement.Replace,
                subdivision: SubdivisionScheme.Quadtree,
                transform: tr
            );

            var tile = s.Get(id);

            // Act

            // Assert
            Assert.That(tile.Index, Is.EqualTo(id));
            Assert.That(tile.GeometricError, Is.EqualTo(7.5));
            Assert.That(tile.Refinement, Is.EqualTo(MethodOfRefinement.Replace));
            Assert.That(tile.Subdivision, Is.EqualTo(SubdivisionScheme.Quadtree));
            Assert.That(tile.Transform, Is.EqualTo(tr));
            // BoundingVolume presence (don’t assert internals here)
            Assert.DoesNotThrow(() =>
            {
                var _ = tile.BoundingVolume;
            });
        }

        [Test]
        public void Tile_Children_And_GetChild_Work_When_TileId_Equals_RangeIndex()
        {
            // Arrange
            // First tile has children [1,2,3,4]; then we actually create those tiles.
            int parent = s.AddTile(Box(), 1.0, ReadOnlySpan<TileContentData>.Empty, new int[] { 1, 2, 3, 4 });
            s.AddTile(Box(), 1.0, ReadOnlySpan<TileContentData>.Empty, ReadOnlySpan<int>.Empty); // id 1
            s.AddTile(Box(), 1.0, ReadOnlySpan<TileContentData>.Empty, ReadOnlySpan<int>.Empty); // id 2
            s.AddTile(Box(), 1.0, ReadOnlySpan<TileContentData>.Empty, ReadOnlySpan<int>.Empty); // id 3
            s.AddTile(Box(), 1.0, ReadOnlySpan<TileContentData>.Empty, ReadOnlySpan<int>.Empty); // id 4

            var tile = s.Get(parent);

            // Act
            var bucket = tile.Children();

            // Assert
            Assert.That(bucket.Count, Is.EqualTo(4));
            Assert.That(bucket[0], Is.EqualTo(1));
            Assert.That(bucket[1], Is.EqualTo(2));
            Assert.That(bucket[2], Is.EqualTo(3));
            Assert.That(bucket[3], Is.EqualTo(4));

            Assert.That(tile.GetChild(0).Index, Is.EqualTo(1));
            Assert.That(tile.GetChild(1).Index, Is.EqualTo(2));
            Assert.That(tile.GetChild(2).Index, Is.EqualTo(3));
            Assert.That(tile.GetChild(3).Index, Is.EqualTo(4));
        }

        [Test]
        public void Tile_GetChild_Uses_Addition_Not_Multiplication_With_NonZero_Offset()
        {
            // Arrange
            // Create an earlier parent with 3 children to advance Flat offset to 3.
            s.AddTile(Box(), 1.0, ReadOnlySpan<TileContentData>.Empty, new int[] { 10, 11, 12 });

            // Real child tiles we’ll point to:
            int c0 = s.AddTile(Box(), 1.0, ReadOnlySpan<TileContentData>.Empty, ReadOnlySpan<int>.Empty);
            int c1 = s.AddTile(Box(), 1.0, ReadOnlySpan<TileContentData>.Empty, ReadOnlySpan<int>.Empty);
            int c2 = s.AddTile(Box(), 1.0, ReadOnlySpan<TileContentData>.Empty, ReadOnlySpan<int>.Empty);
            int c3 = s.AddTile(Box(), 1.0, ReadOnlySpan<TileContentData>.Empty, ReadOnlySpan<int>.Empty);

            // This parent’s children will start at Flat offset 3 (non-zero)
            int parent = s.AddTile(Box(), 1.0, ReadOnlySpan<TileContentData>.Empty, new int[] { c0, c1, c2, c3 });
            var tile = s.Get(parent);

            // Act
            var kids = tile.Children();

            // Assert
            Assert.That(kids.Count, Is.EqualTo(4));
            Assert.That(kids[0], Is.EqualTo(c0));
            Assert.That(kids[1], Is.EqualTo(c1));
            Assert.That(kids[2], Is.EqualTo(c2));
            Assert.That(kids[3], Is.EqualTo(c3));

            // If GetChild uses (offset * i), these will fail
            Assert.That(tile.GetChild(0).Index, Is.EqualTo(c0));
            Assert.That(tile.GetChild(1).Index, Is.EqualTo(c1));
            Assert.That(tile.GetChild(2).Index, Is.EqualTo(c2));
            Assert.That(tile.GetChild(3).Index, Is.EqualTo(c3));
        }

        [Test]
        public void Tile_GetChild_Uses_Addition_With_NonZero_Offset()
        {
            var s = new ColdStorage(AreaOfInterest, initialSize: 64, alloc: Allocator.Temp);

            // First parent to bump the Flat offset (3 children -> offset = 3 for next)
            s.AddTile(Box(), 1.0, ReadOnlySpan<TileContentData>.Empty, new int[] { 10, 11, 12 });

            int c0 = s.AddTile(Box(), 1.0, ReadOnlySpan<TileContentData>.Empty, ReadOnlySpan<int>.Empty);
            int c1 = s.AddTile(Box(), 1.0, ReadOnlySpan<TileContentData>.Empty, ReadOnlySpan<int>.Empty);
            int c2 = s.AddTile(Box(), 1.0, ReadOnlySpan<TileContentData>.Empty, ReadOnlySpan<int>.Empty);
            int c3 = s.AddTile(Box(), 1.0, ReadOnlySpan<TileContentData>.Empty, ReadOnlySpan<int>.Empty);

            int parent = s.AddTile(Box(), 1.0, ReadOnlySpan<TileContentData>.Empty, new int[] { c0, c1, c2, c3 });
            var tile = s.Get(parent);

            var kids = tile.Children();

            Assert.That(kids.Count, Is.EqualTo(4));
            Assert.That(kids[0], Is.EqualTo(c0));
            Assert.That(kids[1], Is.EqualTo(c1));
            Assert.That(kids[2], Is.EqualTo(c2));
            Assert.That(kids[3], Is.EqualTo(c3));

            Assert.That(tile.GetChild(0).Index, Is.EqualTo(c0));
            Assert.That(tile.GetChild(1).Index, Is.EqualTo(c1));
            Assert.That(tile.GetChild(2).Index, Is.EqualTo(c2));
            Assert.That(tile.GetChild(3).Index, Is.EqualTo(c3));

            s.Dispose();
        }

        [Test]
        public void Tile_GetChild_OutOfRange_Throws()
        {
            // Arrange
            int parent = s.AddTile(Box(), 1.0, ReadOnlySpan<TileContentData>.Empty, new int[] { 7, 8, 9 });
            var tile = s.Get(parent);

            // Act + Assert
            Assert.That(() => tile.GetChild(-1), Throws.Exception);
            Assert.That(() => tile.GetChild(3), Throws.Exception); // Count == 3 → last valid is 2
        }

        [Test]
        public void Tile_Children_Empty_Is_Safe()
        {
            // Arrange
            int t = s.AddTile(Box(), 1.0, ReadOnlySpan<TileContentData>.Empty, ReadOnlySpan<int>.Empty);
            var tile = s.Get(t);

            // Act
            var kids = tile.Children();

            // Assert
            Assert.That(kids.Count, Is.EqualTo(0));
            Assert.That(() => tile.GetChild(0), Throws.Exception);
        }
    }
}