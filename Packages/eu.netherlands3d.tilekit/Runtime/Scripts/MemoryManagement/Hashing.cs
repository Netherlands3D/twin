using System;
using System.Runtime.CompilerServices;
using System.Text;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace Netherlands3D.Tilekit.MemoryManagement
{
    // Not multi-thread safe by design - the backing buffer will help reduce memory use and fragmentation.
    public static class Hashing
    {
        // Backing buffer reused for all hash operations.
        private static FixedString4096Bytes buffer;

        // TODO: Can we prevent this allocation using ZString (https://github.com/Cysharp/ZString)?
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint2 HashBytes(ReadOnlySpan<byte> bytes) => HashString(Encoding.UTF8.GetString(bytes));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint2 HashString(string text)
        {
            if (text == null) throw new ArgumentNullException(nameof(text));

            buffer.Clear();
            buffer.Append(text);

            return HashFixedString(buffer);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint2 HashFixedString(FixedString4096Bytes text) => xxHash3.Hash64(text);
    }
}
