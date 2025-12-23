namespace Netherlands3D.Tilekit.Profiling
{
    /// <summary>
    /// Optional global sources (e.g., Texture2DLoader cache).
    /// </summary>
    public interface IGlobalStatsSource
    {
        void Collect(ref GlobalStats stats);
    }
}