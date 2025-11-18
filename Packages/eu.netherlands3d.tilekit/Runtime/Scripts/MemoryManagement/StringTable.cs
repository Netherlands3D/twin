using System;
using System.Runtime.CompilerServices;
using Unity.Collections;

namespace Netherlands3D.Tilekit.MemoryManagement
{
    public sealed class StringTable : IDisposable
    {
        private NativeList<byte> blob;
        private NativeList<int> offsets;
        private NativeList<int> lengths;

        public StringTable(int expectedStrings, int expectedBytes, Allocator alloc)
        {
            blob = new NativeList<byte>(expectedBytes, alloc);
            offsets = new NativeList<int>(expectedStrings, alloc);
            lengths = new NativeList<int>(expectedStrings, alloc);
        }

        public int AddUtf8(ReadOnlySpan<byte> utf8, bool zeroTerminate = true)
        {
            int idx = offsets.Length;
            offsets.AddNoResize(blob.Length);
            lengths.AddNoResize(utf8.Length);
            for (int i = 0; i < utf8.Length; i++) blob.AddNoResize(utf8[i]);
            if (zeroTerminate) blob.AddNoResize(0);
            return idx;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NativeSlice<byte> GetSlice(int strIndex)
        {
            int start = offsets[strIndex];
            int len = lengths[strIndex];
            return new NativeSlice<byte>(blob.AsArray(), start, len);
        }

        /// Tries to read into a FixedString128Bytes. Returns false if truncated.
        public bool TryGetFixedString128(int strIndex, out FixedString128Bytes fs)
        {
            fs = default;
            var slice = GetSlice(strIndex);
            int i = 0;
            int written = 0;
            const int cap = 127; // FixedString128Bytes payload

            while (i < slice.Length && written < cap)
            {
                byte b0 = slice[i];
                if (b0 < 0x80)
                {
                    // ASCII fast-path
                    fs.Append((char)b0);
                    i++;
                    written++;
                    continue;
                }

                // Minimal UTF-8 decode for 2–3 byte sequences (common in names)
                if ((b0 & 0xE0) == 0xC0 && i + 1 < slice.Length)
                {
                    byte b1 = slice[i + 1];
                    int code = ((b0 & 0x1F) << 6) | (b1 & 0x3F);
                    fs.Append((char)code);
                    i += 2;
                    written++;
                    continue;
                }

                if ((b0 & 0xF0) == 0xE0 && i + 2 < slice.Length)
                {
                    byte b1 = slice[i + 1];
                    byte b2 = slice[i + 2];
                    int code = ((b0 & 0x0F) << 12) | ((b1 & 0x3F) << 6) | (b2 & 0x3F);
                    fs.Append((char)code);
                    i += 3;
                    written++;
                    continue;
                }

                // Fallback: skip invalid/4-byte sequences (or implement full UTF-8 if you need it)
                i++;
            }

            // Return false if we truncated
            return i >= slice.Length;
        }


        public void Dispose()
        {
            blob.Dispose();
            offsets.Dispose();
            lengths.Dispose();
        }

        public void Clear()
        {
            blob.Clear();
            offsets.Clear();
            lengths.Clear();
        }
    }
}