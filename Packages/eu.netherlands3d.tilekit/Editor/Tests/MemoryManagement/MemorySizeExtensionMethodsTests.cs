using Netherlands3D.Tilekit.MemoryManagement;
using NUnit.Framework;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Netherlands3D.Tilekit.Tests.MemoryManagement
{
    public class MemorySizeExtensionMethodsTests
    {
        [Test]
        [Category("Unit")]
        public void NativeArray_ReservedEqualsTotalNumberOfBytes()
        {
            var exampleLength = 7;
            long expected = (long)exampleLength * UnsafeUtility.SizeOf<int>();

            var arr = new NativeArray<int>(exampleLength, Allocator.Temp);
            try
            {
                Assert.That(arr.GetReservedBytes(), Is.EqualTo(expected));
            }
            finally
            {
                arr.Dispose();
            }
        }

        [Test]
        [Category("Unit")]
        public void NativeArray_UsedEqualsTotalNumberOfBytes()
        {
            var exampleLength = 7;
            long expected = (long)exampleLength * UnsafeUtility.SizeOf<int>();

            var arr = new NativeArray<int>(exampleLength, Allocator.Temp);
            try
            {
                Assert.That(arr.GetUsedBytes(), Is.EqualTo(expected));

                // Arrays do not track how many elements are actually used; thus it is equal to the number of reserved bytes
                Assert.That(arr.GetUsedBytes(), Is.EqualTo(arr.GetReservedBytes()));
            }
            finally
            {
                arr.Dispose();
            }
        }

        [Test]
        [Category("Unit")]
        public void NativeArray_NumberOfBytesIsZeroWhenNotCreated()
        {
            NativeArray<int> list = default;

            Assert.That(list.GetReservedBytes(), Is.EqualTo(0));
            Assert.That(list.GetUsedBytes(), Is.EqualTo(0));
        }

        [Test]
        [Category("Unit")]
        public void NativeList_ReservedEqualsTotalNumberOfBytes()
        {
            var list = new NativeList<int>(10, Allocator.Temp);
            try
            {
                Assert.That(list.GetReservedBytes(), Is.EqualTo((long)list.Capacity * UnsafeUtility.SizeOf<int>()));
            }
            finally
            {
                list.Dispose();
            }
        }

        [Test]
        [Category("Unit")]
        public void NativeList_UsedEqualsNumberOfBytesOfAddedItems()
        {
            var list = new NativeList<int>(10, Allocator.Temp);
            try
            {
                // Add 3 items
                list.Add(1);
                list.Add(2);
                list.Add(3);

                // Check whether GetUsedBytes() actually is 3 times the size of an int
                Assert.That(list.GetUsedBytes(), Is.EqualTo((long)3 * UnsafeUtility.SizeOf<int>()));
            }
            finally
            {
                list.Dispose();
            }
        }

        [Test]
        [Category("Unit")]
        public void NativeList_UsedIsZeroWhenNotCreated()
        {
            NativeList<int> list = default;

            Assert.That(list.GetReservedBytes(), Is.EqualTo(0));
            Assert.That(list.GetUsedBytes(), Is.EqualTo(0));
        }

        [Test]
        [Category("Unit")]
        public void NativeParallelHashMap_ReservedEqualsTotalNumberOfBytes()
        {
            var map = new NativeParallelHashMap<int, int>(capacity: 16, Allocator.Temp);
            try
            {
                long reserved = map.GetReservedBytes();
                
                Assert.That(reserved, Is.EqualTo(320));
            }
            finally
            {
                map.Dispose();
            }
        }

        [Test]
        [Category("Unit")]
        public void NativeParallelHashMap_UsedEqualsNumberOfBytesOfAddedItems()
        {
            var map = new NativeParallelHashMap<int, int>(capacity: 16, Allocator.Temp);
            try
            {
                map.TryAdd(1, 10);
                map.TryAdd(2, 20);
                map.TryAdd(3, 30);

                long used = map.GetUsedBytes();
                
                Assert.That(used, Is.EqualTo(164));
            }
            finally
            {
                map.Dispose();
            }
        }

        [Test]
        [Category("Unit")]
        public void NativeParallelHashMap_NotCreated_ReturnsZero()
        {
            NativeParallelHashMap<int, int> map = default;

            Assert.That(map.GetReservedBytes(), Is.EqualTo(0));
            Assert.That(map.GetUsedBytes(), Is.EqualTo(0));
        }
    }
}
