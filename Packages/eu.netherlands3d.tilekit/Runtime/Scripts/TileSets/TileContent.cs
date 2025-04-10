using System.Runtime.Serialization;
using KindMen.Uxios;
using Netherlands3D.Tilekit.TileSets.ImplicitTiling;

namespace Netherlands3D.Tilekit.TileSets
{
    [DataContract(Namespace = "eu.netherlands3d.tilekit.tilesets", Name = "TileContent")]
    public struct TileContent
    {
        public TemplatedUri Uri { get; }
        public IBoundingVolume BoundingVolume { get; }
        public Metadata Metadata { get; }

        public TileContent(TemplatedUri uri, IBoundingVolume boundingVolume, Metadata metadata = null)
        {
            Uri = uri;
            BoundingVolume = boundingVolume;
            Metadata = metadata ?? new Metadata();
        }
    }
}