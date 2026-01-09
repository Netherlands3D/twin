#nullable enable

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using Unity.Profiling;
using UnityEngine.Profiling;

namespace Netherlands3D.Tilekit.Profiling
{
    public static class TilekitProfilerCategory
    {
        public static readonly ProfilerCategory Category = new ProfilerCategory("Tilekit");
    }
    
    public static class Telemetry
    {
        // Stable GUID for your module (generate once and keep unchanged).
        private static readonly Guid MetaId = new Guid("B1B9A0E7-6A7B-4A21-9F64-0C8C70D46B2C");
        private const int MetaTag = 1;

        // Registry: rare mutations; per-frame iteration.
        private static readonly List<ITileSetStatsSource> dataSets = new(64);
        private static readonly List<IGlobalStatsSource> globals = new(8);

        // Reused per-frame metadata buffer (managed, but reused; grows rarely).
        private static TileSetMetaRow[]? metaRows;

        private static bool initialized;

        // ---- Global counters ----

        private static readonly ProfilerCounterValue<int> DataSetsCounter = new(
            TilekitProfilerCategory.Category,
            "DataSets",
            ProfilerMarkerDataUnit.Count,
            ProfilerCounterOptions.FlushOnEndOfFrame
        );

        private static readonly ProfilerCounterValue<long> NativeReservedCounter = new(
            TilekitProfilerCategory.Category,
            "Native Reserved (bytes)",
            ProfilerMarkerDataUnit.Bytes,
            ProfilerCounterOptions.FlushOnEndOfFrame
        );

        private static readonly ProfilerCounterValue<long> NativeUsedCounter = new(
            TilekitProfilerCategory.Category,
            "Native Used (bytes)",
            ProfilerMarkerDataUnit.Bytes,
            ProfilerCounterOptions.FlushOnEndOfFrame
        );

        private static readonly ProfilerCounterValue<int> TilesAllocatedCounter = new(
            TilekitProfilerCategory.Category,
            "Tiles Allocated",
            ProfilerMarkerDataUnit.Count,
            ProfilerCounterOptions.FlushOnEndOfFrame
        );

        private static readonly ProfilerCounterValue<int> TilesActualCounter = new(
            TilekitProfilerCategory.Category,
            "Tiles Actual",
            ProfilerMarkerDataUnit.Count,
            ProfilerCounterOptions.FlushOnEndOfFrame
        );

        private static readonly ProfilerCounterValue<int> WarmCounter = new(
            TilekitProfilerCategory.Category,
            "Warm Tiles",
            ProfilerMarkerDataUnit.Count,
            ProfilerCounterOptions.FlushOnEndOfFrame
        );

        private static readonly ProfilerCounterValue<int> HotCounter = new(
            TilekitProfilerCategory.Category,
            "Hot Tiles",
            ProfilerMarkerDataUnit.Count,
            ProfilerCounterOptions.FlushOnEndOfFrame
        );

        private static readonly ProfilerCounterValue<long> TextureCacheBytesCounter = new(
            TilekitProfilerCategory.Category,
            "Texture Cache (bytes)",
            ProfilerMarkerDataUnit.Bytes,
            ProfilerCounterOptions.FlushOnEndOfFrame
        );

        // ---------------- Lifecycle ----------------

        public static void EnsureInitialized()
        {
            if (initialized) return;

            // Reset counters to a known baseline.
            DataSetsCounter.Value = 0;
            NativeReservedCounter.Value = 0;
            NativeUsedCounter.Value = 0;
            TilesAllocatedCounter.Value = 0;
            TilesActualCounter.Value = 0;
            WarmCounter.Value = 0;
            HotCounter.Value = 0;
            TextureCacheBytesCounter.Value = 0;

            initialized = true;
        }

        public static void Shutdown()
        {
            dataSets.Clear();
            globals.Clear();
            metaRows = null;
            initialized = false;

            DataSetsCounter.Value = 0;
        }

        // ---------------- Registration ----------------

        public static void Register(ITileSetStatsSource source)
        {
            EnsureInitialized();
            if (source == null) return;

            for (int i = 0; i < dataSets.Count; i++)
                if (ReferenceEquals(dataSets[i], source))
                    return;

            dataSets.Add(source);
            DataSetsCounter.Value = dataSets.Count;
        }

        public static void Unregister(ITileSetStatsSource source)
        {
            if (source == null) return;

            for (int i = 0; i < dataSets.Count; i++)
            {
                if (!ReferenceEquals(dataSets[i], source)) continue;
                dataSets.RemoveAt(i);
                break;
            }

            DataSetsCounter.Value = dataSets.Count;
        }

        public static void RegisterGlobal(IGlobalStatsSource source)
        {
            EnsureInitialized();
            if (source == null) return;

            for (int i = 0; i < globals.Count; i++)
                if (ReferenceEquals(globals[i], source))
                    return;

            globals.Add(source);
        }

        public static void UnregisterGlobal(IGlobalStatsSource source)
        {
            if (source == null) return;

            for (int i = 0; i < globals.Count; i++)
            {
                if (!ReferenceEquals(globals[i], source)) continue;
                globals.RemoveAt(i);
                break;
            }
        }

        // ---------------- Publishing ----------------

        /// <summary>
        /// Call once per frame (LateUpdate is usually best) to publish counters + metadata.
        /// </summary>
        public static unsafe void EndOfFramePublish()
        {
            if (!initialized) return;

            int count = dataSets.Count;

            // Collect globals (e.g. texture cache)
            var global = default(GlobalStats);
            for (int i = 0; i < globals.Count; i++)
                globals[i].Collect(ref global);

            if (count == 0)
            {
                DataSetsCounter.Value = 0;
                NativeReservedCounter.Value = 0;
                NativeUsedCounter.Value = 0;
                TilesAllocatedCounter.Value = 0;
                TilesActualCounter.Value = 0;
                WarmCounter.Value = 0;
                HotCounter.Value = 0;
                TextureCacheBytesCounter.Value = global.TextureCacheBytes;

                // Emit an empty array (optional, but keeps consumers simple)
                Profiler.EmitFrameMetaData(MetaId, MetaTag, Array.Empty<TileSetMetaRow>());
                return;
            }

            // Ensure metadata buffer is large enough (rare managed allocation on growth).
            if (metaRows == null || metaRows.Length < count)
                metaRows = new TileSetMetaRow[count];

            long totalReserved = 0;
            long totalUsed = 0;
            int totalTilesAllocated = 0;
            int totalTilesActual = 0;
            int totalWarm = 0;
            int totalHot = 0;

            for (int i = 0; i < count; i++)
            {
                var src = dataSets[i];

                var stats = default(TileSetStats);
                src.Collect(ref stats);

                totalReserved += stats.NativeReservedBytes;
                totalUsed += stats.NativeUsedBytes;
                totalTilesAllocated += stats.TilesAllocated;
                totalTilesActual += stats.TilesActual;
                totalWarm += stats.WarmCount;
                totalHot += stats.HotCount;

                fixed (TileSetMetaRow* rowPtr = &metaRows[i])
                {
                    rowPtr->DataSetId = src.DataSetId;

                    WriteNameUtf8(src.DataSetName, rowPtr->NameUtf8, 64);

                    rowPtr->NativeReservedBytes = stats.NativeReservedBytes;
                    rowPtr->NativeUsedBytes = stats.NativeUsedBytes;
                    rowPtr->TilesAllocated = stats.TilesAllocated;
                    rowPtr->TilesActual = stats.TilesActual;
                    rowPtr->StringsAllocated = stats.StringsAllocated;
                    rowPtr->StringsActual = stats.StringsActual;
                    rowPtr->UrisAllocated = stats.UrisAllocated;
                    rowPtr->UrisActual = stats.UrisActual;
                    rowPtr->WarmCount = stats.WarmCount;
                    rowPtr->HotCount = stats.HotCount;
                }
            }

            // Publish global counters
            DataSetsCounter.Value = count;
            NativeReservedCounter.Value = totalReserved;
            NativeUsedCounter.Value = totalUsed;
            TilesAllocatedCounter.Value = totalTilesAllocated;
            TilesActualCounter.Value = totalTilesActual;
            WarmCounter.Value = totalWarm;
            HotCounter.Value = totalHot;
            TextureCacheBytesCounter.Value = global.TextureCacheBytes;

            // Emit metadata rows for this frame
            Profiler.EmitFrameMetaData(MetaId, MetaTag, metaRows);
        }

        // ---------------- Helpers ----------------

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe void WriteNameUtf8(string? name, byte* dst, int dstLen)
        {
            // Zero-fill destination
            for (int i = 0; i < dstLen; i++)
                dst[i] = 0;

            if (string.IsNullOrEmpty(name))
                return;

#if NETSTANDARD2_1_OR_GREATER || UNITY_2021_2_OR_NEWER
            // Encode directly into destination (no allocations on modern Unity profiles)
            var span = new Span<byte>(dst, dstLen);
            int written = Encoding.UTF8.GetBytes(name.AsSpan(), span);

            // Ensure null termination if truncated
            if (written >= dstLen)
                dst[dstLen - 1] = 0;
#else
            // Fallback (allocates). If you're stuck on an older profile and want zero-alloc,
            // replace this with your own encoder that writes into dst.
            byte[] tmp = Encoding.UTF8.GetBytes(name);
            int copy = tmp.Length < dstLen ? tmp.Length : dstLen;
            for (int i = 0; i < copy; i++)
                dst[i] = tmp[i];

            if (copy == dstLen)
                dst[dstLen - 1] = 0;
#endif
        }
    }
    
}
