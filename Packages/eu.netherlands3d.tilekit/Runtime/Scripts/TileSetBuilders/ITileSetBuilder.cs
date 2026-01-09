using Netherlands3D.Tilekit.WriteModel;

namespace Netherlands3D.Tilekit.TileSetBuilders
{
    public interface ITileSetBuilder<in T> where T : struct
    {
        void Build(TileSet tiles, T settings);
    }
}