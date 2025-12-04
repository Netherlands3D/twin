using System;
using Netherlands3D.Tilekit.MemoryManagement;
using NUnit.Framework;
using Unity.Collections;

namespace Netherlands3D.Tilekit.Tests.MemoryManagement
{
    public class BucketTests
    {
        [Test]
        public void FromArray_BasicIndexingAndCount()
        {
            // Arrange
            using var arr = new NativeArray<int>(new[] { 10, 20, 30, 40, 50 }, Allocator.Temp);
            var r = new BucketRange(offset: 1, count: 3);

            // Act
            var bucket = Bucket<int>.From(arr, r);

            // Assert
            Assert.That(bucket.Count, Is.EqualTo(3));
            Assert.That(bucket[0], Is.EqualTo(20));
            Assert.That(bucket[1], Is.EqualTo(30));
            Assert.That(bucket[2], Is.EqualTo(40));
        }

        [Test]
        public void FromArray_EmptyRange_YieldsZeroCount()
        {
            // Arrange
            using var arr = new NativeArray<int>(new[] { 7, 8, 9 }, Allocator.Temp);
            var r = new BucketRange(offset: 2, count: 0);

            // Act
            var bucket = Bucket<int>.From(arr, r);

            // Assert
            Assert.That(bucket.Count, Is.EqualTo(0));
            // enumerating should not throw
            foreach (var _ in bucket) { /* no-op */ }
        }

        [Test]
        public void FromArray_OutOfBoundsIndex_Throws()
        {
            // Arrange
            using var arr = new NativeArray<int>(new[] { 1, 2, 3, 4 }, Allocator.Temp);
            var r = new BucketRange(offset: 1, count: 2); // valid slice [2,3]
            var bucket = Bucket<int>.From(arr, r);

            // Act + Assert
            Assert.That(bucket.Count, Is.EqualTo(2));
            Assert.That(() => { var _ = bucket[2]; }, Throws.TypeOf<IndexOutOfRangeException>());
            Assert.That(() => { var _ = bucket[-1]; }, Throws.TypeOf<IndexOutOfRangeException>());
        }

        [Test]
        public void FromList_BasicIndexingAndEnumeration()
        {
            // Arrange
            using var list = new NativeList<int>(5, Allocator.Temp);
            list.AddNoResize(100);
            list.AddNoResize(200);
            list.AddNoResize(300);
            list.AddNoResize(400);
            list.AddNoResize(500);

            var r = new BucketRange(offset: 2, count: 2);

            // Act
            var bucket = Bucket<int>.From(list, r);

            // Assert
            Assert.That(bucket.Count, Is.EqualTo(2));
            Assert.That(bucket[0], Is.EqualTo(300));
            Assert.That(bucket[1], Is.EqualTo(400));

            var collected = new System.Collections.Generic.List<int>();
            foreach (var v in bucket) collected.Add(v);
            Assert.That(collected, Is.EqualTo(new[] { 300, 400 }));
        }
    }

    /// <summary>
    /// Small helpers to keep tests tidy.
    /// </summary>
    public static class BucketTestExtensions
    {
        public static int[] ToArray(this Bucket<int> bucket)
        {
            var arr = new int[bucket.Count];
            for (int i = 0; i < bucket.Count; i++) arr[i] = bucket[i];
            return arr;
        }
    }
}