namespace Netherlands3D.Tilekit.Profiling
{
    /// <summary>
    /// Implemented by one object per DataSet.
    /// </summary>
    public interface ITileSetStatsSource
    {
        int DataSetId { get; }
        string DataSetName { get; } // UTF8-truncated to 64 bytes in metadata row
        void Collect(ref TileSetStats stats);
    }
}