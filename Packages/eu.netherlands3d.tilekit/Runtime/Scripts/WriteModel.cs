using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Profiling;

namespace Netherlands3D.Tilekit.Optimized
{
    public enum BoundingVolumeType : byte
    {
        Region,
        Sphere,
        Box
    }

    [StructLayout(LayoutKind.Sequential)]
    public readonly struct BoundingVolumeRef // 1 byte (+ 3 B padding) + 1 int = 8 B
    {
        public readonly BoundingVolumeType Type; // 1 B (will pad to 8 on 64-bit; still tiny)
        public readonly int Index; // index into the corresponding pool

        public BoundingVolumeRef(BoundingVolumeType type, int index)
        {
            Type = type;
            Index = index;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public readonly struct BoxBoundingVolume // 12 doubles = 96 B
    {
        public readonly double3 Center;
        public readonly double3 HalfAxisX;
        public readonly double3 HalfAxisY;
        public readonly double3 HalfAxisZ;

        /// <summary>
        /// Total box size derived from the magnitude of the half axes.
        /// </summary>
        public double3 Size => new(math.length(HalfAxisX) * 2.0, math.length(HalfAxisY) * 2.0, math.length(HalfAxisZ) * 2.0);

        /// <summary>
        /// The corner with the smallest x/y/z values.
        /// </summary>
        public double3 TopLeft => Center - Size * 0.5;

        /// <summary>
        /// The corner with the largest x/y/z values.
        /// </summary>
        public double3 BottomRight => Center + Size * 0.5;

        public BoxBoundingVolume(double3 center, double3 halfAxisX, double3 halfAxisY, double3 halfAxisZ)
        {
            Center = center;
            HalfAxisX = halfAxisX;
            HalfAxisY = halfAxisY;
            HalfAxisZ = halfAxisZ;
        }
        
        /// <summary>
        /// Creates a box centered at <paramref name="center"/> with the given <paramref name="size"/>.
        /// </summary>
        public static BoxBoundingVolume FromBounds(double3 center, double3 size)
        {
            return new BoxBoundingVolume(
                center,
                new double3(size.x * 0.5, 0, 0),
                new double3(0, size.y * 0.5, 0),
                new double3(0, 0, size.z * 0.5)
            );
        }
        
        /// <summary>
        /// Creates a box from top-left and bottom-right coordinates (as double3).
        /// </summary>
        public static BoxBoundingVolume FromTopLeftAndBottomRight(double3 topLeft, double3 bottomRight)
        {
            return FromBounds(
                (topLeft + bottomRight) * 0.5, 
                math.abs(bottomRight - topLeft)
            );
        }

        public BoundsDouble ToBounds()
        {
            throw new NotImplementedException();
        }

        public (BoxBoundingVolume tl, BoxBoundingVolume tr, BoxBoundingVolume br, BoxBoundingVolume bl) Subdivide2D()
        {
            var min = TopLeft;
            var max = BottomRight;

            var midX = (min.x + max.x) * 0.5;
            var midY = (min.y + max.y) * 0.5;

            var zMin = min.z;
            var zMax = max.z;

            // tl: x[min, midX], y[min, midY]
            var tl = FromTopLeftAndBottomRight(new double3(min.x, min.y, zMin), new double3(midX, midY, zMax));

            // tr: x[midX, max], y[min, midY]
            var tr = FromTopLeftAndBottomRight(new double3(midX, min.y, zMin), new double3(max.x, midY, zMax));

            // br: x[midX, max], y[midY, max]
            var br = FromTopLeftAndBottomRight(new double3(midX, midY, zMin), new double3(max.x, max.y, zMax));

            // bl: x[min, midX], y[midY, max]
            var bl = FromTopLeftAndBottomRight(new double3(min.x, midY, zMin), new double3(midX, max.y, zMax));

            return (tl, tr, br, bl);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public readonly struct RegionBoundingVolume // 6 doubles = 48 B
    {
        public readonly double West, South, East, North, MinHeight, MaxHeight;

        public RegionBoundingVolume(double west, double south, double east, double north, double minHeight, double maxHeight)
        {
            West = west;
            South = south;
            East = east;
            North = north;
            MinHeight = minHeight;
            MaxHeight = maxHeight;
        }

        public BoundsDouble ToBounds()
        {
            throw new NotImplementedException();
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public readonly struct SphereBoundingVolume // 4 doubles = 32 B
    {
        public readonly double3 Center;
        public readonly double Radius;

        public SphereBoundingVolume(double3 center, double radius)
        {
            Center = center;
            Radius = radius;
        }

        public BoundsDouble ToBounds()
        {
            throw new NotImplementedException();
        }
    }

    public struct BoundingVolumeStore
    {
        // TODO: Change to native lists
        public NativeArray<BoundingVolumeRef> BoundingVolumeRefs;
        public NativeArray<BoxBoundingVolume> Boxes;
        public NativeArray<RegionBoundingVolume> Regions;
        public NativeArray<SphereBoundingVolume> Spheres;

        public void Alloc(int initialSize, Allocator alloc = Allocator.Persistent)
        {
            BoundingVolumeRefs = new NativeArray<BoundingVolumeRef>(initialSize, alloc);
            Boxes = new NativeArray<BoxBoundingVolume>(initialSize, alloc);
            Regions = new NativeArray<RegionBoundingVolume>(initialSize, alloc);
            Spheres = new NativeArray<SphereBoundingVolume>(initialSize, alloc);
        }
        
        public BoundingVolumeRef Add(int idx, BoxBoundingVolume b)
        {
            Boxes[idx] = b;
            BoundingVolumeRefs[idx] = new BoundingVolumeRef(BoundingVolumeType.Box, idx);
            return BoundingVolumeRefs[idx];
        }

        public BoundingVolumeRef Add(int idx, RegionBoundingVolume r)
        {
            Regions[idx] = r;
            BoundingVolumeRefs[idx] = new BoundingVolumeRef(BoundingVolumeType.Region, idx);
            return BoundingVolumeRefs[idx];
        }

        public BoundingVolumeRef Add(int idx, SphereBoundingVolume s)
        {
            Spheres[idx] = s;
            BoundingVolumeRefs[idx] = new BoundingVolumeRef(BoundingVolumeType.Sphere, idx);
            return BoundingVolumeRefs[idx];
        }
    }
}

namespace Netherlands3D.Tilekit.Optimized.TileSets
{
    public enum SubdivisionScheme : byte
    {
        None,
        UniformGrid,
        Quadtree,
        Octree
    }

    public enum MethodOfRefinement : byte
    {
        Add,
        Replace
    }

    public readonly struct BucketRange
    {
        public int Offset { get; }
        public int Count { get; }

        public BucketRange(int offset, int count)
        {
            this.Offset = offset;
            this.Count = count;
        }
    }

    /// NOTE: Buckets created from NativeList<T> are **transient views** over the list's current buffer.
    /// If the list grows (reallocates), previously created buckets/slices become invalid. Use immediately,
    public sealed class Buckets<T> : IDisposable where T : unmanaged
    {
        public NativeList<BucketRange> Ranges;
        public NativeList<T> Flat;

        public Buckets(int expectedRanges, int expectedItems, Allocator alloc)
        {
            Ranges = new NativeList<BucketRange>(expectedRanges, alloc);
            Flat = new NativeList<T>(expectedItems, alloc);
        }

        public int Add(ReadOnlySpan<T> items)
        {
            int idx = Ranges.Length;
            int off = Flat.Length;
            Ranges.AddNoResize(new BucketRange(off, items.Length));
            for (int i = 0; i < items.Length; i++) Flat.AddNoResize(items[i]);
            return idx;
        }

        public Bucket<T> GetBucket(int rangeIndex) => Bucket<T>.From(Flat, Ranges[rangeIndex]);

        public void Clear()
        {
            Ranges.Clear();
            Flat.Clear();
        }

        public void Dispose()
        {
            Clear();
            Ranges.Dispose();
            Flat.Dispose();
        }
    }

    public struct Bucket<T> where T : unmanaged
    {
        private NativeSlice<T> s;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Bucket(NativeSlice<T> slice)
        {
            s = slice;
        }

        public T this[int index] => s[index];
        public int Count => s.Length;

        public NativeSlice<T>.Enumerator GetEnumerator() => s.GetEnumerator();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Bucket<T> From(NativeArray<T> flat, BucketRange r)
            => new(new NativeSlice<T>(flat, r.Offset, r.Count));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Bucket<T> From(NativeList<T> flat, BucketRange r)
            => new(new NativeSlice<T>(flat.AsArray(), r.Offset, r.Count));
    }

    public sealed class StringTable : IDisposable
    {
        public NativeList<byte> Blob;
        public NativeList<int> Offsets;
        public NativeList<int> Lengths;

        public StringTable(int expectedStrings, int expectedBytes, Allocator alloc)
        {
            Blob = new NativeList<byte>(expectedBytes, alloc);
            Offsets = new NativeList<int>(expectedStrings, alloc);
            Lengths = new NativeList<int>(expectedStrings, alloc);
        }

        public int AddUtf8(ReadOnlySpan<byte> utf8, bool zeroTerminate = true)
        {
            int idx = Offsets.Length;
            Offsets.AddNoResize(Blob.Length);
            Lengths.AddNoResize(utf8.Length);
            for (int i = 0; i < utf8.Length; i++) Blob.AddNoResize(utf8[i]);
            if (zeroTerminate) Blob.AddNoResize(0);
            return idx;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NativeSlice<byte> GetSlice(int strIndex)
        {
            int start = Offsets[strIndex];
            int len = Lengths[strIndex];
            return new NativeSlice<byte>(Blob.AsArray(), start, len);
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
            Blob.Dispose();
            Offsets.Dispose();
            Lengths.Dispose();
        }

        public void Clear()
        {
            Blob.Clear();
            Offsets.Clear();
            Lengths.Clear();
        }
    }

    public readonly struct TileContentData
    {
        public readonly int UriIndex; // index into string table
        public readonly BoundingVolumeRef BoundingVolume;

        public TileContentData(int uriIndex, BoundingVolumeRef boundingVolume)
        {
            UriIndex = uriIndex;
            BoundingVolume = boundingVolume;
        }
    }

    /// <summary>
    /// The central storage location for memory related to a tileset
    /// </summary>
    public sealed class TilesStorage : IDisposable
    {
        public BoundingVolumeStore BoundingVolumes;
        public NativeList<double> GeometricError; // hot
        public NativeList<MethodOfRefinement> Refine; // hot/small
        public NativeList<SubdivisionScheme> Subdivision; // small
        public NativeList<float4x4> Transform; // consider sparsifying if many are identity

        public readonly Buckets<int> Children;
        public readonly Buckets<TileContentData> Contents;
        public readonly StringTable Strings;

        // Allocation and growth are aligned to 64 tiles. This ensures memory alignment 
        // and reduces fragmentation when resizing or replacing storages. Because each 
        // storage grows in fixed-size increments, freed blocks have predictable sizes 
        // and can be efficiently reused by the allocator.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        bool IsMultipleOf64(int value) => (value & 63) == 0;

        static readonly ProfilerCounterValue<int> layersCounter = new(
            ProfilerCategory.Scripts, 
            "Tilekit - Number of layers",
            ProfilerMarkerDataUnit.Count, 
            ProfilerCounterOptions.FlushOnEndOfFrame
        );
        static readonly ProfilerCounterValue<int> allocatedTilesCounter = new(
            ProfilerCategory.Scripts, 
            "Tilekit - Allocated tiles",
            ProfilerMarkerDataUnit.Count, 
            ProfilerCounterOptions.FlushOnEndOfFrame
        );
        static readonly ProfilerCounterValue<int> actualTilesCounter = new(
            ProfilerCategory.Scripts, 
            "Tilekit - Actual tiles",
            ProfilerMarkerDataUnit.Count, 
            ProfilerCounterOptions.FlushOnEndOfFrame
        );
        
        public TilesStorage(int initialSize = 64, Allocator alloc = Allocator.Persistent)
        {
            if (!IsMultipleOf64(initialSize))
            {
                throw new ArgumentException("Initial size must be a multiple of 64", nameof(initialSize));
            }

            layersCounter.Value += 1;
            allocatedTilesCounter.Value += initialSize;
            
            BoundingVolumes = new BoundingVolumeStore();
            BoundingVolumes.Alloc(initialSize, alloc);
            GeometricError = new NativeList<double>(initialSize, alloc);
            Refine = new NativeList<MethodOfRefinement>(initialSize, alloc);
            Subdivision = new NativeList<SubdivisionScheme>(initialSize, alloc);
            Transform = new NativeList<float4x4>(initialSize, alloc);

            // Assume 4 children per tile and have the list autogrow. This matches the concept of quad trees, and
            // even though these should be defined as implicit tilesets - it is a useful metric.
            Children = new Buckets<int>(initialSize, initialSize * 4, alloc);

            // Assume that tiles have a single content by default, there could be multiple but generally there is only 1
            Contents = new Buckets<TileContentData>(initialSize, initialSize, alloc);

            // Assume strings have a length of 128 bytes on average
            Strings = new StringTable(initialSize, initialSize * 128, alloc);
        }

        public int AddTile(
            in BoxBoundingVolume boundingVolume,
            double geometricError,
            ReadOnlySpan<TileContentData> contents,
            ReadOnlySpan<int> children = default,
            MethodOfRefinement refine = MethodOfRefinement.Replace,
            SubdivisionScheme subdivision = SubdivisionScheme.None,
            in float4x4 transform = default
        ) {
            // Take any of the arrays whose length matches the number of tiles in this storage and use it's length
            // as the new id as this is last id + 1
            int id = GeometricError.Length;

            actualTilesCounter.Value += 1;
            
            BoundingVolumes.Add(id, boundingVolume);
            GeometricError.AddNoResize(geometricError);
            Refine.AddNoResize(refine);
            Subdivision.AddNoResize(subdivision);
            Transform.AddNoResize(transform);

            Children.Add(children);
            Contents.Add(contents);

            return id;
        }

        public Tile Get(int i)
        {
            return new Tile(this, i);
        }

        public void Dispose()
        {
            layersCounter.Value -= 1;
            allocatedTilesCounter.Value -= GeometricError.Capacity;
            actualTilesCounter.Value -= GeometricError.Length;
            
            GeometricError.Dispose();
            Refine.Dispose();
            Subdivision.Dispose();
            Transform.Dispose();

            Children.Dispose();
            Contents.Dispose();
            Strings.Dispose();
        }

        public void Clear()
        {
            GeometricError.Clear();
            Refine.Clear();
            Subdivision.Clear();
            Transform.Clear();

            Children.Clear();
            Contents.Clear();
            Strings.Clear();
        }
    }
}