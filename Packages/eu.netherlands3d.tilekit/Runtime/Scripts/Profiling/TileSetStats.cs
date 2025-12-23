namespace Netherlands3D.Tilekit.Profiling
{
    public struct TileSetStats
    {
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