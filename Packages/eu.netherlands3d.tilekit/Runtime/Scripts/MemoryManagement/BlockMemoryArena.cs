using System;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace Netherlands3D.Tilekit.MemoryManagement
{
    /// <summary>
    /// An append-only, contiguous memory arena that stores variable-sized blocks in a single flat allocation.
    /// </summary>
    /// <typeparam name="T">The element type stored in the arena.</typeparam>
    /// <remarks>
    /// <para>
    /// Blocks are appended to a flat <see cref="NativeList{T}"/> (<see cref="Items"/>) and their locations are tracked
    /// in a parallel <see cref="NativeList{T}"/> of <see cref="BlockRange"/> entries (<see cref="BlockRanges"/>).
    /// </para>
    /// <para>
    /// Indexing returns a <see cref="BlockMemoryArenaBlock{T}"/> view into the underlying flat storage.
    /// </para>
    /// <para>
    /// <b>Allocation behavior:</b> This type is intended to be used with pre-sized capacities. The <see cref="Add"/>
    /// method uses <c>AddNoResize</c> for performance and will fail if capacity is insufficient.
    /// </para>
    /// </remarks>
    public class BlockMemoryArena<T> : IDisposable, IMemoryReporter where T : unmanaged
    {
        /// <summary>
        /// The ranges that map block ids to offsets and sizes within <see cref="Items"/>.
        /// </summary>
        protected NativeList<BlockMemoryArenaBlockRange> BlockRanges;
        
        /// <summary>
        /// The flat append-only storage that contains all block items contiguously.
        /// </summary>
        protected NativeArray<T> Items;

        private int itemsLength;
        
        /// <summary>
        /// Returns the length of the flat array containing all items that are in use.
        /// </summary>
        private int ItemLength => itemsLength;
        
        /// <summary>
        /// Returns the length of the flat array as a "capacity" - in practice the capacity amount of memory is
        /// allocated, with the length we can do capacity planning to see how much of the capacity is actually used.
        /// </summary>
        private int ItemCapacity => Items.Length;

        /// <summary>
        /// The number of blocks stored in this arena.
        /// </summary>
        public int Length => BlockRanges.Length;
        
        /// <summary>
        /// The currently reserved capacity (in number of block entries) for <see cref="BlockRanges"/>.
        /// </summary>
        public int Capacity => BlockRanges.Capacity;
        
        /// <summary>
        /// Gets a transient view of the block at <paramref name="blockIndex"/>.
        /// </summary>
        public BlockMemoryArenaBlock<T> this[int blockIndex] => BlockMemoryArenaBlock<T>.From(Items, BlockRanges[blockIndex]);
        
        /// <summary>
        /// Creates a new arena with explicit capacities for blocks and items.
        /// </summary>
        /// <param name="blockCapacity">Initial capacity for the number of blocks (<see cref="BlockRanges"/> entries).</param>
        /// <param name="itemCapacity">Initial capacity for the total number of items stored across all blocks.</param>
        /// <param name="alloc">Allocator to use for native containers.</param>
        public BlockMemoryArena(int blockCapacity, int itemCapacity, Allocator alloc = Allocator.Persistent)
        {
            BlockRanges = new NativeList<BlockMemoryArenaBlockRange>(blockCapacity, alloc);
            Items = new NativeArray<T>(itemCapacity, alloc);
            itemsLength = 0;
        }
        
        /// <summary>
        /// Appends a new block to the arena and returns its block id, a block may be empty.
        /// </summary>
        /// <param name="block">The block contents to append.</param>
        /// <returns>The id (index into <see cref="Ranges"/>) for the appended block.</returns>
        /// <remarks>
        /// Uses <c>AddNoResize</c> for performance and therefore requires sufficient pre-allocated capacity in both
        /// <see cref="BlockRanges"/> and <see cref="Items"/>.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual int Add(ReadOnlySpan<T> block)
        {
            if (ItemLength + block.Length > ItemCapacity) throw new InvalidOperationException("arena is full.");

            int newBlockIndex = BlockRanges.Length;
            int firstItemOffset = ItemLength;
            
            // Allocate a contiguous block of memory
            BlockRanges.AddNoResize(new BlockMemoryArenaBlockRange(firstItemOffset, block.Length));
            
            // Copy the data into the allocated block
            for (int i = 0; i < block.Length; i++)
            {
                Items[itemsLength] = block[i];
                itemsLength++;
            }

            return newBlockIndex;
        }

        /// <summary>
        /// Returns the estimated number of bytes reserved (capacity-weighted) by this arena's native containers.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public long GetReservedBytes() => BlockRanges.GetReservedBytes() + Items.GetReservedBytes();

        /// <summary>
        /// Returns the estimated number of bytes currently used (length-weighted) by this arena's native containers.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public long GetUsedBytes() => BlockRanges.GetUsedBytes() + (long)ItemLength * UnsafeUtility.SizeOf<T>();

        /// <summary>
        /// Removes all blocks and items, keeping allocated capacity for reuse.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear()
        {
            BlockRanges.Clear();
            itemsLength = 0;
        }

        /// <summary>
        /// Clears and disposes the native containers owned by this arena.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose()
        {
            Clear(); // explicitly clear to avoid potential memory leaks

            BlockRanges.Dispose();
            Items.Dispose();
        }
    }
}