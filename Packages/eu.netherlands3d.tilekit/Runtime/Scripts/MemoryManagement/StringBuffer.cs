using System;
using System.Runtime.CompilerServices;
using System.Text;
using Unity.Collections;
using Unity.Mathematics;

namespace Netherlands3D.Tilekit.MemoryManagement
{
    /// <summary>
    /// Deduplicating UTF-8 string buffer.
    ///
    /// Stores UTF-8 encoded bytes of each unique string in a Buffer&lt;byte&gt;.
    /// Returns an integer handle (block index) per unique string.
    /// </summary>
    public class StringBuffer : Buffer<byte>, IMemoryReporter
    {
        private NativeParallelHashMap<uint2, int> hashToIndex;

        public StringBuffer(int blockCapacity, int byteCapacity, Allocator allocator)
            : base(blockCapacity, byteCapacity, allocator)
        {
            hashToIndex = new NativeParallelHashMap<uint2, int>(blockCapacity, allocator);
        }

        /// <summary>
        /// Intern a managed string: ensure it exists in the buffer and
        /// return its block index.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Add(string value)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));

            // Encode to UTF-8 bytes
            byte[] tmp = Encoding.UTF8.GetBytes(value);
            return Add(tmp.AsSpan());
        }

        public int Add(ReadOnlySpan<char> characters)
        {
            // copy the utf8 encoded bytes into a span to prevent heap allocations, and still get a good UTF-8 set of bytes.
            Span<byte> buffer = stackalloc byte[4096];
            
            // note: the numberOfBytes is different from the number of chars - each UTF-8 character can be between 1 and 4 bytes. 
            var numberOfBytes = Encoding.UTF8.GetBytes(characters, buffer);

            return Add(buffer[..numberOfBytes]);
        }

        /// <summary>
        /// Intern a UTF-8 byte span: ensure it exists in the buffer and
        /// return its block index.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int Add(ReadOnlySpan<byte> utf8Bytes)
        {
            uint2 hash = Hashing.HashBytes(utf8Bytes);

            if (hashToIndex.TryGetValue(hash, out int index))
            {
                return index;
            }

            // Not found: add new block
            int newIndex = base.Add(utf8Bytes);
            hashToIndex.Add(hash, newIndex);
            return newIndex;
        }

        /// <summary>
        /// Decode a stored block as a managed string.
        /// </summary>
        public string GetAsString(int index)
        {
            if ((uint)index >= (uint)Ranges.Length)
                throw new ArgumentOutOfRangeException(nameof(index));

            var range = Ranges[index];

            // Build a temporary byte[]
            byte[] tmp = new byte[range.Count];
            int offset = range.Offset;
            for (int i = 0; i < range.Count; i++)
            {
                tmp[i] = Items[offset + i];
            }

            return Encoding.UTF8.GetString(tmp);
        }

        /// <summary>
        /// Gives you a slice of the underlying UTF-8 bytes for this block.
        /// Useful if you want to avoid creating strings.
        /// </summary>
        public NativeSlice<byte> GetAsBytes(int index)
        {
            if ((uint)index >= (uint)Ranges.Length)
                throw new ArgumentOutOfRangeException(nameof(index));

            var range = Ranges[index];
            return new NativeSlice<byte>(Items, range.Offset, range.Count);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public new long GetReservedBytes() => hashToIndex.GetReservedBytes() + Ranges.GetReservedBytes() + Items.GetReservedBytes();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public new long GetUsedBytes() => hashToIndex.GetUsedBytes() + Ranges.GetUsedBytes() + Items.GetUsedBytes();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public new void Clear()
        {
            base.Clear();
            hashToIndex.Clear();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public new void Dispose()
        {
            if (hashToIndex.IsCreated)
                hashToIndex.Dispose();

            base.Dispose();
        }
    }
}