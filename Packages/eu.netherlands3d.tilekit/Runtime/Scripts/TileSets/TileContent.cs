using Unity.Collections;

namespace Netherlands3D.Tilekit.TileSets
{
    public struct TileContent
    {
        public NativeText UriTemplate;
        public BoundingVolume BoundingVolume;

        public TileContent(NativeText uriTemplate, BoundingVolume boundingVolume)
        {
            UriTemplate = uriTemplate;
            BoundingVolume = boundingVolume;
        }
    }
}