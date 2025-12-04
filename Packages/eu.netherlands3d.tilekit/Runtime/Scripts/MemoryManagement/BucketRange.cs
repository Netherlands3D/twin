namespace Netherlands3D.Tilekit.MemoryManagement
{
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
}