using Netherlands3D.Tilekit.WriteModel;

namespace Netherlands3D.Tilekit.ColdStorageMaterializers
{
    public interface IColdStorageMaterializer<in T> where T : struct
    {
        void Materialize(ColdStorage tiles, T settings);
    }
}