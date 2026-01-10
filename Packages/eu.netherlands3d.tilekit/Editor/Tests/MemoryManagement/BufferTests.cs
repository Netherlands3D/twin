using System;
using Netherlands3D.Tilekit.MemoryManagement;
using NUnit.Framework;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Netherlands3D.Tilekit.Tests.MemoryManagement
{
    public class BufferTests
    {
        [Test]
        [Category("Scenario")]
        public void Scenario_TrackChildrenIndices()
        {
            // Scenario:
            // Given a set of tiles with children
            // And each tile can have a variable number of children,
            // When using a block-based buffer to store child indices,
            // Then each block contains the children indices for a single tile.

            // 1. Create a buffer that can hold up to 8 blocks (tiles) and at most 32 indices total.
            // Beware: a Buffer is an IDisposable, usually you want to use a Using statement to ensure it is disposed,
            // for demonstration purposes we don't and dispose manually.
            var children = new Buffer<int>(blockCapacity: 8, itemCapacity: 32, alloc: Allocator.Temp);

            // 2. Add a (root) tile's 3 children and get an integer id representing the list of children.
            int rootTile = children.Add(stackalloc int[] { 1, 2 });
            Assert.That(rootTile, Is.EqualTo(0));
            
            // 3. Add another tile's children - if you keep the id of the blocks in sync with the tile id, then you
            //    even have a hierarchy because this line will state that tile with id 1 has children with ids 4 and 5.
            //    and the tile with id 1 was added in the previous statement with the root tile
            int firstChild = children.Add(stackalloc int[] { 4, 5, 6 });
            Assert.That(firstChild, Is.EqualTo(1));
            
            // 4. Suppose the second child does not have its own children, we could even append an empty block. This way
            //    the tile id and the block id can remain in sync.
            int secondChild = children.Add(stackalloc int[] {});
            Assert.That(secondChild, Is.EqualTo(2));
            
            // 5. Let's iterate through the list of children for the root tile
            var rootChildren = children[rootTile];
            for (int i = 0; i < rootChildren.Length; i++)
            {
                Assert.That(rootChildren[i], Is.EqualTo(i + 1));
            }

            // 6. Let's pretend we want to interact with this as a hierarchy, let's see how many children the first and second child of
            //    the root tile have.
            Assert.That(children[rootChildren[0]].Length, Is.EqualTo(3));
            Assert.That(children[rootChildren[1]].Length, Is.EqualTo(0));
            
            // 7. Clear the buffer to make it ready for re-use/.
            children.Clear();
            Assert.That(children.Length, Is.EqualTo(0));
            
            // 8. Dispose the buffer to free up memory when needed
            children.Dispose();
        }
        
        [Test]
        [Category("Unit")]
        public void Add_ReturnsSequentialIds_AndDataIsReadable()
        {
            using var buffer = new Buffer<int>(blockCapacity: 8, itemCapacity: 32, alloc: Allocator.Temp);

            int idA = buffer.Add(stackalloc int[] { 1, 2, 3 });
            int idB = buffer.Add(stackalloc int[] { 10, 20 });

            Assert.That(idA, Is.EqualTo(0));
            Assert.That(idB, Is.EqualTo(1));
            Assert.That(buffer.Length, Is.EqualTo(2));

            var a = buffer[idA];
            var b = buffer[idB];

            Assert.That(a.Length, Is.EqualTo(3));
            Assert.That(a[0], Is.EqualTo(1));
            Assert.That(a[1], Is.EqualTo(2));
            Assert.That(a[2], Is.EqualTo(3));

            Assert.That(b.Length, Is.EqualTo(2));
            Assert.That(b[0], Is.EqualTo(10));
            Assert.That(b[1], Is.EqualTo(20));
        }

        [Test]
        [Category("Unit")]
        public void Add_EmptySpanReturnsAnEmptyBlock()
        {
            using var buffer = new Buffer<int>(blockCapacity: 8, itemCapacity: 32, alloc: Allocator.Temp);

            long usedBefore = buffer.GetUsedBytes();
            int id = buffer.Add(ReadOnlySpan<int>.Empty);

            Assert.That(id, Is.EqualTo(0));
            Assert.That(buffer.Length, Is.EqualTo(1));
            Assert.That(buffer.GetUsedBytes(), Is.EqualTo(usedBefore + UnsafeUtility.SizeOf<BufferBlockRange>()));
        }

        [Test]
        [Category("Unit")]
        public void Add_ThrowsWhenItemsCapacityExceeded()
        {
            using var buffer = new Buffer<int>(blockCapacity: 8, itemCapacity: 4, alloc: Allocator.Temp);

            var index = buffer.Add(stackalloc int[] { 1, 2, 3, 4 });
            Assert.That(index, Is.EqualTo(0));
            Assert.That(buffer.Length, Is.EqualTo(1));

            Assert.That(
                () => buffer.Add(stackalloc int[] { 5 }),
                Throws.TypeOf<InvalidOperationException>()
            );
        }

        [Test]
        [Category("Unit")]
        public void Clear_ResetsLength_ButKeepsCapacity()
        {
            using var buffer = new Buffer<int>(blockCapacity: 4, itemCapacity: 16, alloc: Allocator.Temp);

            buffer.Add(stackalloc int[] { 1, 2, 3 });
            int capBefore = buffer.Capacity;

            buffer.Clear();

            Assert.That(buffer.Length, Is.EqualTo(0));
            Assert.That(buffer.Capacity, Is.EqualTo(capBefore));
        }

        [Test]
        [Category("Unit")]
        public void MemoryReporter_ReservedAtLeastUsed()
        {
            var nativeListBaseMemoryUse = 16;
            var blockCapacity = 4;
            var itemCapacity = 16;
            Span<int> items = stackalloc int[] { 1, 2, 3 };
            
            using var buffer = new Buffer<int>(blockCapacity, itemCapacity, alloc: Allocator.Temp);
            buffer.Add(items);

            long reserved = buffer.GetReservedBytes();
            long used = buffer.GetUsedBytes();

            Assert.That(reserved, Is.EqualTo(nativeListBaseMemoryUse + itemCapacity*UnsafeUtility.SizeOf<int>() + nativeListBaseMemoryUse+blockCapacity*UnsafeUtility.SizeOf<BufferBlockRange>()));
            Assert.That(used, Is.EqualTo(buffer.Length * UnsafeUtility.SizeOf<BufferBlockRange>() + items.Length * UnsafeUtility.SizeOf<int>()));
        }

        [Test]
        [Category("Unit")]
        public void Dispose_CanBeCalledAfterClear()
        {
            var buffer = new Buffer<int>(blockCapacity: 4, itemCapacity: 16, alloc: Allocator.Temp);
            buffer.Add(stackalloc int[] { 1, 2, 3 });

            buffer.Clear();

            Assert.That(() => buffer.Dispose(), Throws.Nothing);
        }
    }
}
