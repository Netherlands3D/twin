namespace Netherlands3D.Tilekit.MemoryManagement
{
    public readonly struct BlockRange
    {
        public int Offset { get; }
        public int Count { get; }

        public BlockRange(int offset, int count)
        {
            this.Offset = offset;
            this.Count = count;
        }
    }
}