using System;
using System.Runtime.InteropServices;

namespace Netherlands3D.Tilekit.MemoryManagement
{
    /// <summary>
    /// Describes a contiguous range (offset and count) inside a flat, append-only buffer.
    /// </summary>
    /// <remarks>
    /// A <see cref="BufferBlockRange"/> is typically used to locate a "block" of items that was appended to a flat
    /// <c>NativeArray</c> or <c>NativeList</c>.
    /// </remarks>
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct BufferBlockRange
    {
        /// <summary>
        /// The zero-based offset into the flat buffer where the block starts.
        /// </summary>
        public int Offset { get; }

        /// <summary>
        /// The number of items in the block.
        /// </summary>
        public int Count { get; }

        /// <summary>
        /// Creates a new range describing a block starting at <paramref name="offset"/> with
        /// <paramref name="count"/> items.
        /// </summary>
        /// <param name="offset">Zero-based start index in the flat buffer.</param>
        /// <param name="count">Number of items in the block, allowed to be zero if the block is indicative of an item without, for example, children.</param>
        public BufferBlockRange(int offset, int count)
        {
            if (offset < 0) throw new ArgumentOutOfRangeException(nameof(offset));
            if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));
            
            Offset = offset;
            Count = count;
        }
    }
}