using System.Runtime.InteropServices;

namespace Netherlands3D.Tilekit.Profiling
{
    /// <summary>
    /// One row per TileSet. Must be blittable for EmitFrameMetaData.
    /// Name is fixed-size UTF-8 (zero padded).
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct TileSetMetaRow
    {
        public int DataSetId;
        public fixed byte NameUtf8[64];

        public long NativeReservedBytes;
        public long NativeUsedBytes;

        public int TilesAllocated;
        public int TilesActual;

        public int StringsAllocated;
        public int StringsActual;

        public int UrisAllocated;
        public int UrisActual;

        public int WarmCount;
        public int HotCount;
    }
}