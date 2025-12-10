using System;
using Netherlands3D.Tilekit.MemoryManagement;
using NUnit.Framework;
using Unity.Collections;

namespace Netherlands3D.Tilekit.Tests.MemoryManagement
{
    public class BufferTests
    {
        [Test]
        public void Add_ReturnsRangeIndex_AndStoresFlatDataInOrder()
        {
            // Arrange
            using var buckets = new Buffer<int>(blockCapacity: 20, itemCapacity: 80, alloc: Allocator.Temp);

            ReadOnlySpan<int> a = stackalloc int[] { 1, 2, 3 };
            ReadOnlySpan<int> b = stackalloc int[] { 10, 20 };
            ReadOnlySpan<int> c = stackalloc int[] { 7, 8, 9, 10 };

            // Act
            int ia = buckets.Add(a);
            int ib = buckets.Add(b);
            int ic = buckets.Add(c);

            // Assert (range indices)
            Assert.That(ia, Is.EqualTo(0));
            Assert.That(ib, Is.EqualTo(1));
            Assert.That(ic, Is.EqualTo(2));

            // Assert (ranges)
            var ra = buckets.Ranges[ia];
            var rb = buckets.Ranges[ib];
            var rc = buckets.Ranges[ic];

            Assert.That(ra.Offset, Is.EqualTo(0));
            Assert.That(ra.Count, Is.EqualTo(3));

            Assert.That(rb.Offset, Is.EqualTo(3));
            Assert.That(rb.Count, Is.EqualTo(2));

            Assert.That(rc.Offset, Is.EqualTo(5));
            Assert.That(rc.Count, Is.EqualTo(4));

            // Assert (flat buffer content)
            Assert.That(buckets.Items.Length, Is.EqualTo(3 + 2 + 4));
            Assert.That(buckets.Items[0], Is.EqualTo(1));
            Assert.That(buckets.Items[1], Is.EqualTo(2));
            Assert.That(buckets.Items[2], Is.EqualTo(3));
            Assert.That(buckets.Items[3], Is.EqualTo(10));
            Assert.That(buckets.Items[4], Is.EqualTo(20));
            Assert.That(buckets.Items[5], Is.EqualTo(7));
            Assert.That(buckets.Items[6], Is.EqualTo(8));
            Assert.That(buckets.Items[7], Is.EqualTo(9));
            Assert.That(buckets.Items[8], Is.EqualTo(10));
        }

        [Test]
        public void GetBucket_ReturnsCorrectSlice_ForEachRange()
        {
            // Arrange
            using var buckets = new Buffer<int>(blockCapacity: 20, itemCapacity: 80, alloc: Allocator.Temp);

            int i0 = buckets.Add(stackalloc int[] { 4, 5, 6 });
            int i1 = buckets.Add(stackalloc int[] { 9 });
            int i2 = buckets.Add(stackalloc int[] { 1, 2, 3, 4 });
            int i3 = buckets.Add(stackalloc int[] { });

            // Act
            var b0 = buckets[i0];
            var b1 = buckets[i1];
            var b2 = buckets[i2];
            var b3 = buckets[i3];

            // Assert
            Assert.That(b0.Length, Is.EqualTo(3));
            Assert.That(b0[0], Is.EqualTo(4));
            Assert.That(b0[1], Is.EqualTo(5));
            Assert.That(b0[2], Is.EqualTo(6));

            Assert.That(b1.Length, Is.EqualTo(1));
            Assert.That(b1[0], Is.EqualTo(9));

            Assert.That(b2.Length, Is.EqualTo(4));
            Assert.That(new[] { b2[0], b2[1], b2[2], b2[3] }, Is.EqualTo(new[] { 1, 2, 3, 4 }));

            Assert.That(b3.Length, Is.EqualTo(0));
            foreach (var _ in b3) { /* should not iterate */ }
        }

        [Test]
        public void Clear_ResetsRangesAndFlatLengths()
        {
            // Arrange
            using var buckets = new Buffer<int>(blockCapacity: 2, itemCapacity: 8, alloc: Allocator.Temp);
            buckets.Add(stackalloc int[] { 1, 2, 3 });
            buckets.Add(stackalloc int[] { 4, 5 });

            // Act
            buckets.Clear();

            // Assert
            Assert.That(buckets.Ranges.Length, Is.EqualTo(0));
            Assert.That(buckets.Items.Length, Is.EqualTo(0));

            // Ability to reuse after Clear (capacity remains)
            buckets.Add(stackalloc int[] { 7 });
            Assert.That(buckets.Ranges.Length, Is.EqualTo(1));
            Assert.That(buckets.Items.Length, Is.EqualTo(1));
            Assert.That(buckets.Items[0], Is.EqualTo(7));
        }

        [Test]
        public void MultipleAdds_OffsetsAccumulateCorrectly()
        {
            // Arrange
            using var buckets = new Buffer<int>(blockCapacity: 5, itemCapacity: 16, alloc: Allocator.Temp);

            // Act
            int i0 = buckets.Add(stackalloc int[] { 1, 1, 1, 1 });          // off 0, len 4
            int i1 = buckets.Add(stackalloc int[] { 2 });                    // off 4, len 1
            int i2 = buckets.Add(stackalloc int[] { 3, 3, 3 });              // off 5, len 3
            int i3 = buckets.Add(stackalloc int[] { 4, 4 });                 // off 8, len 2
            int i4 = buckets.Add(stackalloc int[] { 5, 5, 5, 5, 5, 5 });     // off 10, len 6

            // Assert
            Assert.That(buckets.Ranges[i0].Offset, Is.EqualTo(0));
            Assert.That(buckets.Ranges[i1].Offset, Is.EqualTo(4));
            Assert.That(buckets.Ranges[i2].Offset, Is.EqualTo(5));
            Assert.That(buckets.Ranges[i3].Offset, Is.EqualTo(8));
            Assert.That(buckets.Ranges[i4].Offset, Is.EqualTo(10));

            Assert.That(buckets.Ranges[i0].Count, Is.EqualTo(4));
            Assert.That(buckets.Ranges[i1].Count, Is.EqualTo(1));
            Assert.That(buckets.Ranges[i2].Count, Is.EqualTo(3));
            Assert.That(buckets.Ranges[i3].Count, Is.EqualTo(2));
            Assert.That(buckets.Ranges[i4].Count, Is.EqualTo(6));

            // Verify a couple of sample values in Flat at the offsets
            Assert.That(buckets.Items[0], Is.EqualTo(1));
            Assert.That(buckets.Items[4], Is.EqualTo(2));
            Assert.That(buckets.Items[5], Is.EqualTo(3));
            Assert.That(buckets.Items[8], Is.EqualTo(4));
            Assert.That(buckets.Items[10], Is.EqualTo(5));
        }

        [Test]
        public void Disposing_MarksListsAsDisposed()
        {
            // Arrange
            var buckets = new Buffer<int>(blockCapacity: 1, itemCapacity: 1, alloc: Allocator.Temp);

            // Act
            buckets.Dispose();

            // Assert
            Assert.That(buckets.Ranges.IsCreated, Is.False);
            Assert.That(buckets.Items.IsCreated, Is.False);
        }

        [Test]
        public void ZeroLengthAdd_CreatesEmptyRange_WithStableOffsets()
        {
            // Arrange
            using var buckets = new Buffer<int>(blockCapacity: 3, itemCapacity: 4, alloc: Allocator.Temp);

            // Act
            int i0 = buckets.Add(stackalloc int[] { 9, 9 });     // off 0, len 2
            int i1 = buckets.Add(ReadOnlySpan<int>.Empty);       // off 2, len 0
            int i2 = buckets.Add(stackalloc int[] { 7, 7 });     // off 2, len 2

            // Assert
            var r0 = buckets.Ranges[i0];
            var r1 = buckets.Ranges[i1];
            var r2 = buckets.Ranges[i2];

            Assert.That((r0.Offset, r0.Count), Is.EqualTo((0, 2)));
            Assert.That((r1.Offset, r1.Count), Is.EqualTo((2, 0)));
            Assert.That((r2.Offset, r2.Count), Is.EqualTo((2, 2)));

            // And bucket access behaves
            var b0 = buckets.GetBlockById(i0);
            var b1 = buckets.GetBlockById(i1);
            var b2 = buckets.GetBlockById(i2);

            Assert.That(b0.Length, Is.EqualTo(2));
            Assert.That(b1.Length, Is.EqualTo(0));
            Assert.That(b2.Length, Is.EqualTo(2));
            Assert.That(b0[0], Is.EqualTo(9));
            Assert.That(b2[1], Is.EqualTo(7));
        }

        [Test]
        public void BucketEnumerators_WorkForAllRanges()
        {
            // Arrange
            using var buckets = new Buffer<int>(blockCapacity: 3, itemCapacity: 10, alloc: Allocator.Temp);
            int i0 = buckets.Add(stackalloc int[] { 1, 2, 3 });
            int i1 = buckets.Add(stackalloc int[] { 4 });
            int i2 = buckets.Add(stackalloc int[] { 5, 6 });

            // Act
            var e0 = buckets.GetBlockById(i0).ToArray();
            var e1 = buckets.GetBlockById(i1).ToArray();
            var e2 = buckets.GetBlockById(i2).ToArray();

            // Assert
            Assert.That(e0, Is.EqualTo(new[] { 1, 2, 3 }));
            Assert.That(e1, Is.EqualTo(new[] { 4 }));
            Assert.That(e2, Is.EqualTo(new[] { 5, 6 }));
        }
    }
}