namespace Netherlands3D.Tilekit.MemoryManagement
{
    public interface IMemoryReporter
    {
        public long GetReservedBytes();

        public long GetUsedBytes();
    }
}