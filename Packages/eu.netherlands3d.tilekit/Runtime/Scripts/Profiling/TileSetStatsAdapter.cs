using Netherlands3D.Tilekit.WriteModel;

namespace Netherlands3D.Tilekit.Profiling
{
    
    public sealed class TileSetStatsAdapter : ITileSetStatsSource
    {
        private readonly int id;
        private readonly string name;
        private readonly TileSet tileSet;

        public TileSetStatsAdapter(int dataSetId, string dataSetName, TileSet tileSet)
        {
            id = dataSetId;
            name = dataSetName;
            this.tileSet = tileSet;
        }

        public int DataSetId => id;
        public string DataSetName => name;

        public void Collect(ref TileSetStats stats)
        {
            if (tileSet == null) return;
            tileSet.Collect(ref stats);
        }
    }

}