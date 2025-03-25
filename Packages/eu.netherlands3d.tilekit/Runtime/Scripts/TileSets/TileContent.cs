using System.Runtime.Serialization;
using KindMen.Uxios;

namespace Netherlands3D.Tilekit.TileSets
{
    [DataContract(Namespace = "eu.netherlands3d.tilekit.tilesets", Name = "TileContent")]
    public class TileContent
    {
        public TemplatedUri Uri;
        public BoundingVolume BoundingVolume;
        public Metadata Metadata = new();
    }
}