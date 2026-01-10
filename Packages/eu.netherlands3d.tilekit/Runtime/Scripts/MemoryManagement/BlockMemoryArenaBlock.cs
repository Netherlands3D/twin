using System;
using System.Runtime.CompilerServices;
using Unity.Collections;

namespace Netherlands3D.Tilekit.MemoryManagement
{
    /// <summary>
    /// A lightweight, non-allocating view over a contiguous block within a flat arena.
    /// </summary>
    /// <typeparam name="T">The element type stored in the underlying Unity native container.</typeparam>
    /// <remarks>
    /// <para>
    /// This type wraps a <see cref="NativeSlice{T}"/> and is intended to be used as a transient "block view"
    /// into a larger append-only arena (<see cref="NativeArray{T}"/> or <see cref="NativeList{T}"/>).
    /// </para>
    /// <para>
    /// <b>Lifetime:</b> When created from a <see cref="NativeList{T}"/>, the slice becomes invalid if the list
    /// reallocates (e.g., growth beyond capacity). Prefer using it immediately and avoid storing it long-term.
    /// </para>
    /// </remarks>
    public struct BlockMemoryArenaBlock<T> where T : unmanaged
    {
        private NativeSlice<T> s;

        /// <summary>
        /// Gets the element at <paramref name="index"/> within the block.
        /// </summary>
        public T this[int index] => s[index];

        /// <summary>
        /// The number of elements in this block.
        /// </summary>
        public int Length => s.Length;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private BlockMemoryArenaBlock(NativeSlice<T> slice)
        {
            s = slice;
        }

        /// <summary>
        /// Returns a struct enumerator over the block elements.
        /// </summary>
        /// <remarks>
        /// This enumerator is allocation-free and can be used in <c>foreach</c>.
        /// </remarks>
        public NativeSlice<T>.Enumerator GetEnumerator() => s.GetEnumerator();

        /// <summary>
        /// Creates a block from a flat <see cref="NativeArray{T}"/> using the provided range.
        /// </summary>
        /// <param name="flat">The flat arena that contains the block.</param>
        /// <param name="r">The offset and count describing the block.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static BlockMemoryArenaBlock<T> From(NativeArray<T> flat, BlockMemoryArenaBlockRange r)
            => new(new NativeSlice<T>(flat, r.Offset, r.Count));

        /// <summary>
        /// Creates a block from a flat <see cref="NativeList{T}"/> using the provided range.
        /// </summary>
        /// <param name="flat">The list whose underlying arena contains the block.</param>
        /// <param name="r">The offset and count describing the block.</param>
        /// <remarks>
        /// The returned block is a transient view. If <paramref name="flat"/> grows beyond capacity and reallocates,
        /// previously created blocks become invalid.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static BlockMemoryArenaBlock<T> From(NativeList<T> flat, BlockMemoryArenaBlockRange r) => From(flat.AsArray(), r);

        /// <summary>
        /// Replaces the contents of this block with values from <paramref name="replacement"/>.
        ///
        /// This can be used to allocate a block before a tree traversal, and populate the block while traversing
        /// the tree or after the tree traversal. Not all tree traversal algorithms lend themselves to having the
        /// elements up front easily.
        /// </summary>
        /// <param name="replacement">The new values to copy into the block.</param>
        /// <exception cref="System.ArgumentException">
        /// Thrown when <paramref name="replacement"/> does not have the same length as this block.
        /// </exception>
        /// <remarks>
        /// This performs an element-by-element copy into the underlying native container.
        /// </remarks>
        public void Replace(NativeArray<T> replacement)
        {
            if (replacement.Length != s.Length)
            {
                throw new ArgumentException("Replacing a bucket is only possible if the number of children matches the bucket's size.");
            }

            for (int i = 0; i < replacement.Length; i++)
            {
                s[i] = replacement[i];
            }
        }
    }
}