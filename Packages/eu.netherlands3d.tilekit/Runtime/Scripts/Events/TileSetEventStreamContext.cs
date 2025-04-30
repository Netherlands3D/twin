using Netherlands3D.Tilekit;

namespace Netherlands3D.Twin.Tilekit.Events
{
    public struct TileSetEventStreamContext
    {
        public string EventStreamId { get; }

        public TileSet TileSet { get; }

        public TileSetEventStreamContext(string eventStreamId, TileSet tileSet)
        {
            EventStreamId = eventStreamId;
            TileSet = tileSet;
        }
    }
}