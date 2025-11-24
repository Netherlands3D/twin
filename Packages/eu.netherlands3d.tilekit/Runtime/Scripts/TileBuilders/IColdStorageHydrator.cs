using Netherlands3D.Tilekit.WriteModel;

namespace Netherlands3D.Tilekit.TileBuilders
{
    public interface IColdStorageHydrator<in T> where T : struct
    {
        void Build(ColdStorage tiles, T settings);
    }
}