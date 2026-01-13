namespace Netherlands3D.Tilekit.MemoryManagement
{
    /// <summary>
    /// Provides basic memory reporting for native containers owned by a service or data structure.
    /// </summary>
    /// <remarks>
    /// Values are typically estimates of payload bytes and do not include allocator bookkeeping overhead.
    /// </remarks>
    public interface IMemoryReporter
    {
        /// <summary>
        /// Returns the number of bytes reserved by the underlying data structure (capacity-weighted).
        /// </summary>
        public long GetReservedBytes();

        /// <summary>
        /// Returns the number of bytes currently used by the underlying data structure (length/count-weighted).
        /// </summary>
        public long GetUsedBytes();
    }
}