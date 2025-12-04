using Netherlands3D.Tilekit.WriteModel;

namespace Netherlands3D.Tilekit.TileSetMaterializers
{
    public interface ITileSetMaterializer<in T> where T : struct
    {
        void Materialize(TileSet tiles, T settings);
    }
}