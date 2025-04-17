using KindMen.Uxios;

namespace Netherlands3D.Tilekit.TileSets
{
    public struct TileContent
    {
        public TemplatedUri Uri { get; }
        public IBoundingVolume BoundingVolume { get; }

        public TileContent(TemplatedUri uri, IBoundingVolume boundingVolume)
        {
            Uri = uri;
            BoundingVolume = boundingVolume;
        }
    }
}