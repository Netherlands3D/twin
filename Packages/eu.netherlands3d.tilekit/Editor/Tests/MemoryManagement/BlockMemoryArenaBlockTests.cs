using System;
using Netherlands3D.Tilekit.MemoryManagement;
using NUnit.Framework;
using Unity.Collections;

namespace Netherlands3D.Tilekit.Tests.MemoryManagement
{
    public class BlockMemoryArenaBlockTests
    {
        [Test]
        [Category("Unit")]
        public void Length_EqualsRangeCount_ForNativeArray()
        {
            var arr = new NativeArray<int>(10, Allocator.Temp);
            try
            {
                var range = new BlockMemoryArenaBlockRange(offset: 2, count: 4);
                var block = BlockMemoryArenaBlock<int>.From(arr, range);

                Assert.That(block.Length, Is.EqualTo(range.Count));
            }
            finally
            {
                arr.Dispose();
            }
        }

        [Test]
        [Category("Unit")]
        public void Indexer_ReturnsCorrectElements_ForNativeArray()
        {
            var arr = new NativeArray<int>(10, Allocator.Temp);
            try
            {
                for (int i = 0; i < arr.Length; i++) arr[i] = 100 + i; // 100..109

                var block = BlockMemoryArenaBlock<int>.From(arr, new BlockMemoryArenaBlockRange(offset: 3, count: 4)); // 103..106

                Assert.That(block[0], Is.EqualTo(103));
                Assert.That(block[1], Is.EqualTo(104));
                Assert.That(block[2], Is.EqualTo(105));
                Assert.That(block[3], Is.EqualTo(106));
            }
            finally
            {
                arr.Dispose();
            }
        }

        [Test]
        [Category("Unit")]
        public void Indexer_Throws_WhenIndexIsNegative()
        {
            var arr = new NativeArray<int>(10, Allocator.Temp);
            try
            {
                var block = BlockMemoryArenaBlock<int>.From(arr, new BlockMemoryArenaBlockRange(offset: 0, count: 3));

                Assert.That(() => { _ = block[-1]; }, Throws.TypeOf<IndexOutOfRangeException>());
            }
            finally
            {
                arr.Dispose();
            }
        }

        [Test]
        [Category("Unit")]
        public void Indexer_Throws_WhenIndexEqualsLength()
        {
            var arr = new NativeArray<int>(10, Allocator.Temp);
            try
            {
                var block = BlockMemoryArenaBlock<int>.From(arr, new BlockMemoryArenaBlockRange(offset: 0, count: 3));

                Assert.That(() => { _ = block[block.Length]; }, Throws.TypeOf<IndexOutOfRangeException>());
            }
            finally
            {
                arr.Dispose();
            }
        }

        [Test]
        [Category("Unit")]
        public void Length_EqualsRangeCount_ForNativeList()
        {
            var list = new NativeList<int>(10, Allocator.Temp);
            try
            {
                for (int i = 0; i < 10; i++) list.Add(i);

                var range = new BlockMemoryArenaBlockRange(offset: 4, count: 5);
                var block = BlockMemoryArenaBlock<int>.From(list, range);

                Assert.That(block.Length, Is.EqualTo(range.Count));
            }
            finally
            {
                list.Dispose();
            }
        }

        [Test]
        [Category("Unit")]
        public void Indexer_ReturnsCorrectElements_ForNativeList()
        {
            var list = new NativeList<int>(10, Allocator.Temp);
            try
            {
                for (int i = 0; i < 10; i++) list.Add(200 + i); // 200..209

                var block = BlockMemoryArenaBlock<int>.From(list, new BlockMemoryArenaBlockRange(offset: 6, count: 3)); // 206..208

                Assert.That(block[0], Is.EqualTo(206));
                Assert.That(block[1], Is.EqualTo(207));
                Assert.That(block[2], Is.EqualTo(208));
            }
            finally
            {
                list.Dispose();
            }
        }

        [Test]
        [Category("Unit")]
        public void UnderlyingNativeArrayChange_IsVisibleThroughBlock()
        {
            var arr = new NativeArray<int>(8, Allocator.Temp);
            try
            {
                for (int i = 0; i < arr.Length; i++) arr[i] = i; // 0..7

                var block = BlockMemoryArenaBlock<int>.From(arr, new BlockMemoryArenaBlockRange(offset: 2, count: 3)); // arr[2..4]

                // Sanity: initial snapshot through block
                Assert.That(block[0], Is.EqualTo(2));
                Assert.That(block[1], Is.EqualTo(3));
                Assert.That(block[2], Is.EqualTo(4));

                // Mutate underlying array inside the slice range
                arr[3] = 999;

                // Read again through the block: must reflect updated value
                Assert.That(block[1], Is.EqualTo(999));
            }
            finally
            {
                arr.Dispose();
            }
        }
        
        [Test]
        [Category("Unit")]
        public void FromNativeArray_ExposesCorrectWindow()
        {
            var arr = new NativeArray<int>(10, Allocator.Temp);
            try
            {
                for (int i = 0; i < arr.Length; i++) arr[i] = i * 10;

                var block = BlockMemoryArenaBlock<int>.From(arr, new BlockMemoryArenaBlockRange(2, 4));

                Assert.That(block.Length, Is.EqualTo(4));
                Assert.That(block[0], Is.EqualTo(20));
                Assert.That(block[1], Is.EqualTo(30));
                Assert.That(block[2], Is.EqualTo(40));
                Assert.That(block[3], Is.EqualTo(50));
            }
            finally
            {
                arr.Dispose();
            }
        }

        [Test]
        [Category("Unit")]
        public void FromNativeList_ExposesCorrectWindow()
        {
            var list = new NativeList<int>(10, Allocator.Temp);
            try
            {
                for (int i = 0; i < 10; i++) list.Add(i + 1); // 1..10

                var block = BlockMemoryArenaBlock<int>.From(list, new BlockMemoryArenaBlockRange(3, 3)); // 4,5,6

                Assert.That(block.Length, Is.EqualTo(3));
                Assert.That(block[0], Is.EqualTo(4));
                Assert.That(block[1], Is.EqualTo(5));
                Assert.That(block[2], Is.EqualTo(6));
            }
            finally
            {
                list.Dispose();
            }
        }

        [Test]
        [Category("Unit")]
        public void Enumerator_IteratesAllElements()
        {
            var arr = new NativeArray<int>(6, Allocator.Temp);
            try
            {
                for (int i = 0; i < arr.Length; i++) arr[i] = i + 1; // 1..6

                var block = BlockMemoryArenaBlock<int>.From(arr, new BlockMemoryArenaBlockRange(1, 3)); // 2,3,4

                int sum = 0;
                foreach (var v in block) sum += v;

                Assert.That(sum, Is.EqualTo(2 + 3 + 4));
            }
            finally
            {
                arr.Dispose();
            }
        }

        [Test]
        [Category("Unit")]
        public void Replace_CopiesValuesIntoUnderlyingStorage()
        {
            var arr = new NativeArray<int>(8, Allocator.Temp);
            var replacement = new NativeArray<int>(3, Allocator.Temp);
            try
            {
                for (int i = 0; i < arr.Length; i++) arr[i] = -1;

                replacement[0] = 10;
                replacement[1] = 20;
                replacement[2] = 30;

                var block = BlockMemoryArenaBlock<int>.From(arr, new BlockMemoryArenaBlockRange(3, 3));
                block.Replace(replacement);

                Assert.That(arr[3], Is.EqualTo(10));
                Assert.That(arr[4], Is.EqualTo(20));
                Assert.That(arr[5], Is.EqualTo(30));
            }
            finally
            {
                replacement.Dispose();
                arr.Dispose();
            }
        }

        [Test]
        [Category("Unit")]
        public void Replace_ThrowsOnLengthMismatch()
        {
            var arr = new NativeArray<int>(8, Allocator.Temp);
            var replacement = new NativeArray<int>(2, Allocator.Temp);
            try
            {
                var block = BlockMemoryArenaBlock<int>.From(arr, new BlockMemoryArenaBlockRange(3, 3));

                Assert.That(() => block.Replace(replacement), Throws.TypeOf<ArgumentException>());
            }
            finally
            {
                replacement.Dispose();
                arr.Dispose();
            }
        }
        
        [Test]
        [Category("Unit")]
        public void CreatingBufferBlock_DoesNotAllocateManagedMemory()
        {
            var arr = new NativeArray<int>(1024, Allocator.Temp);
            try
            {
                // Warm up JIT / static initialization noise.
                _ = BlockMemoryArenaBlock<int>.From(arr, new BlockMemoryArenaBlockRange(0, 1));

                long before = GC.GetAllocatedBytesForCurrentThread();

                // Create many blocks to amplify any accidental allocation.
                for (int i = 0; i < 10_000; i++)
                {
                    _ = BlockMemoryArenaBlock<int>.From(arr, new BlockMemoryArenaBlockRange(0, 1));
                }

                long after = GC.GetAllocatedBytesForCurrentThread();

                Assert.That(
                    after - before, 
                    Is.EqualTo(0), 
                    "Creating BufferBlock views should not allocate managed memory."
                );
            }
            finally
            {
                arr.Dispose();
            }
        }
    }
}
