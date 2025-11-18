using Netherlands3D.Tilekit.BoundingVolumes;

namespace Netherlands3D.Tilekit
{
    public readonly struct TileContentData
    {
        public readonly int UriIndex; // index into string table
        public readonly BoundingVolumeRef BoundingVolume;

        public TileContentData(int uriIndex, BoundingVolumeRef boundingVolume)
        {
            UriIndex = uriIndex;
            BoundingVolume = boundingVolume;
        }
    }
}